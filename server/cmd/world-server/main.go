package main

import (
	"context"
	"fmt"
	"net"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/jackc/pgx/v5/pgxpool"
	"go.uber.org/zap"
	"google.golang.org/grpc"
	"google.golang.org/grpc/reflection"

	worldHandler "server/internal/world/handler"
	worldRepo "server/internal/world/repository"
	worldSvc "server/internal/world/service"
	"server/pkg/config"
	"server/pkg/database"
	"server/pkg/logger"
	"server/pkg/i18n"
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

	log.Info("Starting World Server",
		zap.String("env", cfg.App.Env),
		zap.String("addr", cfg.Server.Addr),
	)
	if err := i18n.GetBundle().LoadLocales("assets/locales"); err != nil {
		log.Warn("Failed to load locales", zap.Error(err))
	} else {
		log.Info("Locales loaded successfully")
	}

	ctx := context.Background()
	var db *pgxpool.Pool
	for i := 0; i < 5; i++ {
		db, err = database.NewPool(ctx, cfg.GameDB.DSN, log)
		if err == nil {
			break
		}
		log.Warn("Game DB not ready, retrying...", zap.Int("attempt", i+1), zap.Error(err))
		time.Sleep(3 * time.Second)
	}
	if err != nil {
		log.Fatal("Failed to connect to Game DB", zap.Error(err))
	}
	defer db.Close()

	repo := worldRepo.NewPlayerRepository(db)
	svc := worldSvc.New(repo, cfg.Server.ServerID)
	h := worldHandler.New(svc, log)

	grpcServer := grpc.NewServer(
		grpc.UnaryInterceptor(loggingInterceptor(log)),
	)
	pb.RegisterWorldServiceServer(grpcServer, h)
	pb.RegisterGatewayServiceServer(grpcServer, h)

	if cfg.IsDev() {
		reflection.Register(grpcServer)
	}

	lis, err := net.Listen("tcp", cfg.Server.Addr)
	if err != nil {
		log.Fatal("Failed to listen", zap.Error(err))
	}

	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)

	go func() {
		log.Info("World Server ready", zap.String("addr", cfg.Server.Addr))
		if err := grpcServer.Serve(lis); err != nil {
			log.Fatal("gRPC server error", zap.Error(err))
		}
	}()

	<-quit
	log.Info("Shutting down World Server...")
	grpcServer.GracefulStop()
	log.Info("World Server stopped")
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
