package main

import (
	"net"
	"os"

	"server/pkg/config"
	"server/pkg/logger"
	"server/pkg/pb"
	combatHandler "server/internal/combat/handler"
	combatSvc "server/internal/combat/service"
	"go.uber.org/zap"
	"google.golang.org/grpc"
	"google.golang.org/grpc/reflection"
)

func main() {
	cfg, _ := config.Load()
	log := logger.Init(cfg)
	defer logger.Sync()

	port := os.Getenv("COMBAT_SERVER_PORT")
	if port == "" {
		port = "50054"
	}

	lis, err := net.Listen("tcp", ":"+port)
	if err != nil {
		log.Fatal("Failed to listen", zap.Error(err))
	}

	grpcServer := grpc.NewServer()

	svc := combatSvc.NewCombatService(log)
	h := combatHandler.NewCombatHandler(svc, log)
	pb.RegisterCombatServiceServer(grpcServer, h)

	if cfg.IsDev() {
		reflection.Register(grpcServer)
	}

	log.Info("Combat Server is running", zap.String("port", port))
	if err := grpcServer.Serve(lis); err != nil {
		log.Fatal("Failed to serve", zap.Error(err))
	}
}
