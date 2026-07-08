package proxy

import (
	"context"
	"fmt"
	"strconv"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
	"google.golang.org/grpc/metadata"

	"server/internal/gateway/middleware"
	pb "server/pkg/pb"
)

type WorldClient struct {
	pb.WorldServiceClient
	pb.GatewayServiceClient
	conn *grpc.ClientConn
}

func ForwardMetadataInterceptor() grpc.UnaryClientInterceptor {
	return func(
		ctx context.Context,
		method string,
		req, reply interface{},
		cc *grpc.ClientConn,
		invoker grpc.UnaryInvoker,
		opts ...grpc.CallOption,
	) error {
		if userID, ok := middleware.GetUserID(ctx); ok {
			ctx = metadata.AppendToOutgoingContext(ctx, "user-id", strconv.FormatInt(userID, 10))
		}
		return invoker(ctx, method, req, reply, cc, opts...)
	}
}

func NewWorldClient(addr string) (*WorldClient, error) {
	conn, err := grpc.NewClient(addr,
		grpc.WithTransportCredentials(insecure.NewCredentials()),
		grpc.WithUnaryInterceptor(ForwardMetadataInterceptor()),
	)
	if err != nil {
		return nil, fmt.Errorf("connect to world server at %s: %w", addr, err)
	}

	return &WorldClient{
		WorldServiceClient:   pb.NewWorldServiceClient(conn),
		GatewayServiceClient: pb.NewGatewayServiceClient(conn),
		conn:                 conn,
	}, nil
}

func (c *WorldClient) Close() {
	if c.conn != nil {
		c.conn.Close()
	}
}
