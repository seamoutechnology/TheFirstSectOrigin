package main

import (
	"context"
	"encoding/json"
	"fmt"
	"net"
	"net/http"
	"os"
	"os/signal"
	"strings"
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

	// Khởi chạy Monitor Service giám sát tài nguyên và tự động co giãn
	monitorSvc := authSvc.NewMonitorService(zoneRepo, log)
	monitorSvc.Start(ctx)

	// Khởi chạy HTTP Server cho Dashboard giám sát ở cổng 8082
	go func() {
		mux := http.NewServeMux()
		mux.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
			w.Header().Set("Content-Type", "text/html; charset=utf-8")
			w.Write([]byte(authSvc.DashboardHTML))
		})
		
		mux.HandleFunc("/api/status", func(w http.ResponseWriter, r *http.Request) {
			zones, err := zoneRepo.FindActive(ctx)
			if err != nil {
				http.Error(w, err.Error(), 500)
				return
			}
			
			type UIZone struct {
				ID         int     `json:"id"`
				Name       string  `json:"name"`
				CPU        float64 `json:"cpu"`
				RAM        float64 `json:"ram"`
				Players    int     `json:"players"`
				MaxPlayers int     `json:"max_players"`
				Status     string  `json:"status"`
				Gateway    string  `json:"gateway"`
			}
			
			res := make([]UIZone, 0, len(zones))
			for _, z := range zones {
				res = append(res, UIZone{
					ID:         z.ID,
					Name:       z.Name,
					CPU:        z.CPUUsage,
					RAM:        z.RAMUsage,
					Players:    z.PlayerCount,
					MaxPlayers: z.MaxPlayers,
					Status:     z.Status,
					Gateway:    z.GatewayURL,
				})
			}
			
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(res)
		})
		
		mux.HandleFunc("/api/spike", func(w http.ResponseWriter, r *http.Request) {
			if r.Method != http.MethodPost {
				http.Error(w, "Method not allowed", 405)
				return
			}
			
			// Authorization check
			adminSecret := os.Getenv("ADMIN_SECRET")
			if adminSecret == "" {
				adminSecret = "dev_admin_secret"
			}
			
			apiKey := r.Header.Get("X-API-Key")
			if apiKey == "" {
				authHeader := r.Header.Get("Authorization")
				if strings.HasPrefix(authHeader, "Bearer ") {
					apiKey = strings.TrimPrefix(authHeader, "Bearer ")
				}
			}
			if apiKey == "" {
				apiKey = r.URL.Query().Get("secret")
			}
			
			clientIP, _, err := net.SplitHostPort(r.RemoteAddr)
			if err != nil {
				clientIP = r.RemoteAddr
			}
			
			if apiKey != adminSecret {
				log.Warn("Unauthorized access attempt to /api/spike", zap.String("ip", clientIP))
				http.Error(w, "Unauthorized - Invalid API Key", http.StatusUnauthorized)
				return
			}
			
			zoneIDStr := r.URL.Query().Get("zone_id")
			cpuStr := r.URL.Query().Get("cpu")
			
			var zoneID int
			var cpu float64
			fmt.Sscanf(zoneIDStr, "%d", &zoneID)
			fmt.Sscanf(cpuStr, "%f", &cpu)
			
			if zoneID > 0 && cpu > 0 {
				monitorSvc.SetSimulatedCpuSpike(zoneID, cpu)
				log.Info("Set simulated CPU spike (authorized)", zap.String("ip", clientIP), zap.Int("zone_id", zoneID), zap.Float64("cpu", cpu))
				w.WriteHeader(http.StatusOK)
				w.Write([]byte("OK"))
				return
			}
			http.Error(w, "Bad request", 400)
		})
		
		log.Info("Starting Monitor Dashboard Server on port :8082")
		if err := http.ListenAndServe(":8082", mux); err != nil {
			log.Error("Monitor Dashboard Server failed", zap.Error(err))
		}
	}()

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
