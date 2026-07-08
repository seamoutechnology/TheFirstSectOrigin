package proxy

import (
	"context"
	"fmt"
	"time"

	"go.uber.org/zap"
	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
	pb "server/pkg/pb"
)

type combatClient struct {
	pb.CombatServiceClient
	conn *grpc.ClientConn
	log  *zap.Logger
}

func NewCombatClient(addr string, log *zap.Logger) (ICombatClient, error) {
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	conn, err := grpc.DialContext(
		ctx,
		addr,
		grpc.WithTransportCredentials(insecure.NewCredentials()),
		grpc.WithBlock(),
		grpc.WithUnaryInterceptor(ForwardMetadataInterceptor()),
	)
	if err != nil {
		return nil, fmt.Errorf("failed to connect to combat server %s: %w", addr, err)
	}

	log.Info("Connected to Combat Server", zap.String("addr", addr))

	return &combatClient{
		CombatServiceClient: pb.NewCombatServiceClient(conn),
		conn:                conn,
		log:                 log,
	}, nil
}

func (c *combatClient) Close() {
	if c.conn != nil {
		c.conn.Close()
	}
}
