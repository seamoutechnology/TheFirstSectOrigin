package handler

import (
	"context"

	"go.uber.org/zap"
	"google.golang.org/grpc/metadata"
	"server/internal/world/service"
	pb "server/pkg/pb"
	"strconv"
)

type WorldHandler struct {
	pb.UnimplementedWorldServiceServer
	pb.UnimplementedGatewayServiceServer // Thêm để dùng chung handler nếu cần
	svc *service.WorldService
	log *zap.Logger
}

func New(svc *service.WorldService, log *zap.Logger) *WorldHandler {
	return &WorldHandler{svc: svc, log: log}
}

func errBase(msgId string) *pb.BaseResponse {
	return &pb.BaseResponse{Code: 401, Message: msgId}
}

func (h *WorldHandler) InternalSyncSect(ctx context.Context, req *pb.SectInfo) (*pb.BaseResponse, error) {
	return &pb.BaseResponse{Code: 0, Message: "msg_synced"}, nil
}

func (h *WorldHandler) getUserID(ctx context.Context) (int64, bool) {
	md, ok := metadata.FromIncomingContext(ctx)
	if !ok {
		return 0, false
	}
	vals := md.Get("user-id")
	if len(vals) > 0 {
		id, err := strconv.ParseInt(vals[0], 10, 64)
		if err == nil {
			return id, true
		}
	}
	return 0, false
}
