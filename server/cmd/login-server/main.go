package main

import (
	"context"
	"fmt"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/improbable-eng/grpc-web/go/grpcweb"
	"github.com/jackc/pgx/v5/pgxpool"
	"go.uber.org/zap"
	"golang.org/x/net/http2"
	"golang.org/x/net/http2/h2c"
	"google.golang.org/grpc"
	"google.golang.org/grpc/reflection"

	authHandler "server/internal/auth/handler"
	authRepo "server/internal/auth/repository"
	authSvc "server/internal/auth/service"
	"server/pkg/config"
	"server/pkg/database"
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

	log.Info("Starting Login Server",
		zap.String("env", cfg.App.Env),
		zap.String("addr", cfg.Server.Addr),
	)

	ctx := context.Background()
	var db *pgxpool.Pool
	for i := 0; i < 5; i++ {
		db, err = database.NewPool(ctx, cfg.Postgres.DSN, log)
		if err == nil {
			break
		}
		log.Warn("Database not ready, retrying...", zap.Int("attempt", i+1), zap.Error(err))
		time.Sleep(3 * time.Second)
	}
	if err != nil {
		log.Fatal("Failed to connect to database", zap.Error(err))
	}
	defer db.Close()

	redisClient, err := database.NewRedisClient(cfg.Redis.Addr, cfg.Redis.Password, cfg.Redis.DB, log)
	if err != nil {
		log.Fatal("Failed to connect to Redis", zap.Error(err))
	}
	defer redisClient.Close()

	userRepo := authRepo.NewUserRepository(db)
	zoneRepo := authRepo.NewZoneRepository(db)
	jwtMgr := jwtutil.New(cfg.JWT.Secret, cfg.JWT.ExpireHours)
	svc := authSvc.New(userRepo, zoneRepo, jwtMgr, redisClient)
	h := authHandler.New(svc, log)

	grpcServer := grpc.NewServer(
		grpc.UnaryInterceptor(loggingInterceptor(log)),
	)
	pb.RegisterAuthServiceServer(grpcServer, h)

	if cfg.IsDev() {
		reflection.Register(grpcServer)
		log.Info("gRPC reflection enabled (dev mode)")
	}

	wrappedGrpc := grpcweb.WrapServer(grpcServer,
		grpcweb.WithOriginFunc(func(origin string) bool { return true }), // Allow CORS
	)

	log.Info("Login Server ready (gRPC-Web enabled)", zap.String("addr", cfg.Server.Addr))
	
	// Dùng http server để hỗ trợ cả gRPC và gRPC-Web
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

	go func() {
		if err := httpServer.ListenAndServe(); err != nil {
			log.Fatal("HTTP/gRPC server error", zap.Error(err))
		}
	}()

	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit
	log.Info("Shutting down Login Server...")
	grpcServer.GracefulStop()
	log.Info("Login Server stopped")
}

func loggingInterceptor(log *zap.Logger) grpc.UnaryServerInterceptor {
	return func(
		ctx context.Context,
		req interface{},
		info *grpc.UnaryServerInfo,
		handler grpc.UnaryHandler,
	) (interface{}, error) {
		start := time.Now()
		resp, err := handler(ctx, req)
		duration := time.Since(start)

		if err != nil {
			log.Error("gRPC call failed",
				zap.String("method", info.FullMethod),
				zap.Duration("duration", duration),
				zap.Error(err),
			)
		} else {
			log.Info("gRPC call",
				zap.String("method", info.FullMethod),
				zap.Duration("duration", duration),
			)
		}
		return resp, err
	}
}
