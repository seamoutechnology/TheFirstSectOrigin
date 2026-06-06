package proxy

import (
	pb "server/pkg/pb"
)

type IWorldClient interface {
	pb.WorldServiceClient
	pb.GatewayServiceClient
	Close()
}

type ICombatClient interface {
	pb.CombatServiceClient
	Close()
}
