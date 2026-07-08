package main

import (
	"context"
	"fmt"
	"net/http"
	"os"
	"os/signal"
	"strconv"
	"syscall"
	"time"

	"github.com/improbable-eng/grpc-web/go/grpcweb"
	"github.com/prometheus/client_golang/prometheus/promhttp"
	"go.uber.org/zap"
	"golang.org/x/net/http2"
	"golang.org/x/net/http2/h2c"
	"golang.org/x/time/rate"
	"google.golang.org/grpc"
	"google.golang.org/grpc/reflection"

	gatewayHandler "server/internal/gateway/handler"
	"server/internal/gateway/middleware"
	"server/internal/gateway/proxy"
	"server/pkg/config"
	"server/pkg/database"
	"server/pkg/i18n"
	"server/pkg/jwtutil"
	"server/pkg/logger"
	pb "server/pkg/pb"
)

func main() {
	cfg, err := config.Load()
	if err != nil {
		fmt.Printf("Failed to load config: %v\n", err)
		os.Exit(1)
	}
	log := logger.Init(cfg)
	defer logger.Sync()

	if err := i18n.GetBundle().LoadLocales("assets/locales"); err != nil {
		log.Warn("Failed to load locales", zap.Error(err))
	} else {
		log.Info("Locales loaded successfully")
	}

	log.Info("Starting Gateway Server",
		zap.String("env", cfg.App.Env),
		zap.String("addr", cfg.Server.Addr),
	)

	worldAddr := fmt.Sprintf("%s:%d", cfg.Server.Host, cfg.Server.Port+1) // World chạy trên port +1
	if v := os.Getenv("WORLD_SERVER_HOST"); v != "" {
		worldPort := os.Getenv("WORLD_SERVER_PORT")
		if worldPort == "" {
			worldPort = "50053"
		}
		worldAddr = fmt.Sprintf("%s:%s", v, worldPort)
	}

	log.Info("Connecting to World Server", zap.String("addr", worldAddr))
	worldClient, err := proxy.NewWorldClient(worldAddr)
	if err != nil {
		log.Fatal("Failed to connect to World Server", zap.Error(err))
	}
	defer worldClient.Close()

	combatAddr := fmt.Sprintf("%s:%d", cfg.Server.Host, 50054)
	if v := os.Getenv("COMBAT_SERVER_HOST"); v != "" {
		combatPort := os.Getenv("COMBAT_SERVER_PORT")
		if combatPort == "" {
			combatPort = "50054"
		}
		combatAddr = fmt.Sprintf("%s:%s", v, combatPort)
	}

	log.Info("Connecting to Combat Server", zap.String("addr", combatAddr))
	combatClient, err := proxy.NewCombatClient(combatAddr, log)
	if err != nil {
		log.Fatal("Failed to connect to Combat Server", zap.Error(err))
	}
	defer combatClient.Close()

	jwtMgr := jwtutil.New(cfg.JWT.Secret, cfg.JWT.ExpireHours)

	redisClient, err := database.NewRedisClient(cfg.Redis.Addr, cfg.Redis.Password, cfg.Redis.DB, log)
	if err != nil {
		log.Fatal("Failed to connect to Redis", zap.Error(err))
	}
	defer redisClient.Close()

	h := gatewayHandler.New(worldClient, combatClient, log)

	rateLimitRPS := 20.0
	rateLimitBurst := 50
	rateLimitEnabled := true

	if enabledStr := os.Getenv("RATE_LIMIT_ENABLED"); enabledStr != "" {
		if enabled, err := strconv.ParseBool(enabledStr); err == nil {
			rateLimitEnabled = enabled
		}
	}
	if rpsStr := os.Getenv("RATE_LIMIT_RPS"); rpsStr != "" {
		if rps, err := strconv.ParseFloat(rpsStr, 64); err == nil {
			rateLimitRPS = rps
		}
	}
	if burstStr := os.Getenv("RATE_LIMIT_BURST"); burstStr != "" {
		if burst, err := strconv.Atoi(burstStr); err == nil {
			rateLimitBurst = burst
		}
	}

	metricsInterceptor := middleware.MetricsInterceptor()

	go func() {
		http.Handle("/metrics", promhttp.Handler())
		log.Info("Prometheus metrics listening on :9090")
		if err := http.ListenAndServe(":9090", nil); err != nil {
			log.Error("Metrics server failed", zap.Error(err))
		}
	}()

	go func() {
		mux := http.NewServeMux()

		RegisterCutsceneHTTPHandler(mux, worldClient, log)

		fs := http.FileServer(http.Dir("assets"))
		mux.Handle("/assets/", http.StripPrefix("/assets/", fs))

		log.Info("HTTP Static server listening on :8080")
		if err := http.ListenAndServe(":8080", mux); err != nil {
			log.Error("Static server failed", zap.Error(err))
		}
	}()

	var interceptors []grpc.UnaryServerInterceptor
	interceptors = append(interceptors, loggingInterceptor(log))
	interceptors = append(interceptors, metricsInterceptor)

	if rateLimitEnabled {
		log.Info("Rate limiter enabled", zap.Float64("rps", rateLimitRPS), zap.Int("burst", rateLimitBurst))
		rateLimiter := middleware.NewRateLimiter(rate.Limit(rateLimitRPS), rateLimitBurst)
		interceptors = append(interceptors, rateLimiter.UnaryInterceptor())
	} else {
		log.Info("Rate limiter is disabled")
	}

	interceptors = append(interceptors, middleware.AuthInterceptor(jwtMgr, redisClient, log))

	grpcServer := grpc.NewServer(
		grpc.ChainUnaryInterceptor(interceptors...),
	)
	pb.RegisterGatewayServiceServer(grpcServer, h)

	if cfg.IsDev() {
		reflection.Register(grpcServer)
		log.Info("gRPC reflection enabled (dev mode)")
	}

	wrappedGrpc := grpcweb.WrapServer(grpcServer,
		grpcweb.WithOriginFunc(func(origin string) bool { return true }), // Allow CORS
	)

	// DÃ¹ng http server Ä‘á»ƒ há»— trá»£ cáº£ gRPC vÃ  gRPC-Web
	httpServer := &http.Server{
		Addr: cfg.Server.Addr,
		Handler: h2c.NewHandler(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if wrappedGrpc.IsGrpcWebRequest(r) {
				wrappedGrpc.ServeHTTP(w, r)
				return
			}
			grpcServer.ServeHTTP(w, r)
		}), &http2.Server{}),
	}

	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)

	go func() {
		log.Info("Gateway Server ready (gRPC-Web enabled)", zap.String("addr", cfg.Server.Addr))
		if err := httpServer.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Fatal("gRPC server error", zap.Error(err))
		}
	}()

	<-quit
	log.Info("Shutting down Gateway Server...")
	httpServer.Shutdown(context.Background())
	grpcServer.GracefulStop()
	log.Info("Gateway Server stopped")
}

func loggingInterceptor(log *zap.Logger) grpc.UnaryServerInterceptor {
	return func(ctx context.Context, req interface{}, info *grpc.UnaryServerInfo, handler grpc.UnaryHandler) (interface{}, error) {
		start := time.Now()
		resp, err := handler(ctx, req)
		log.Info("gRPC call",
			zap.String("method", info.FullMethod),
			zap.Duration("duration", time.Since(start)),
			zap.Error(err),
		)
		return resp, err
	}
}
