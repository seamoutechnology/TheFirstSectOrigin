package handler

import (
	"context"

	"go.uber.org/zap"
	pb "server/pkg/pb"
)

func (h *WorldHandler) GetVersion(ctx context.Context, req *pb.GetVersionRequest) (*pb.GetVersionResponse, error) {
	h.log.Info("GetVersion request", zap.String("platform", req.Platform))

	cfg, err := h.svc.GetVersionConfig(ctx, req.Platform)
	if err != nil {
		h.log.Error("Failed to get version config", zap.Error(err))
		return &pb.GetVersionResponse{Code: 5000, MessageId: "msg_internal_error"}, nil
	}

	return &pb.GetVersionResponse{
		Code:      0,
		MessageId: "msg_success",
		Config: &pb.VersionConfig{
			ClientVersion:      cfg.ClientVersion,
			AddressableVersion: cfg.AddressableVersion,
			CatalogUrl:         cfg.CatalogURL,
			ForceUpdate:        cfg.ForceUpdate,
			UpdateDesc:         cfg.UpdateDesc,
		},
	}, nil
}


func (h *WorldHandler) ListCutscenes(ctx context.Context, req *pb.EmptyRequest) (*pb.ListCutscenesResponse, error) {
	ids, err := h.svc.ListCutscenes(ctx)
	if err != nil {
		h.log.Error("Failed to list cutscenes", zap.Error(err))
		return nil, err
	}
	return &pb.ListCutscenesResponse{Ids: ids}, nil
}

func (h *WorldHandler) GetCutscene(ctx context.Context, req *pb.GetCutsceneRequest) (*pb.CutsceneData, error) {
	jsonData, err := h.svc.GetCutscene(ctx, req.Id)
	if err != nil {
		h.log.Error("Failed to get cutscene", zap.String("id", req.Id), zap.Error(err))
		return nil, err
	}
	return &pb.CutsceneData{Id: req.Id, JsonContent: jsonData}, nil
}

func (h *WorldHandler) SaveCutscene(ctx context.Context, req *pb.SaveCutsceneRequest) (*pb.BaseResponse, error) {
	if req.Cutscene == nil || req.Cutscene.Id == "" {
		return &pb.BaseResponse{Code: 400, Message: "Invalid Request"}, nil
	}
	err := h.svc.SaveCutscene(ctx, req.Cutscene.Id, req.Cutscene.JsonContent)
	if err != nil {
		h.log.Error("Failed to save cutscene", zap.String("id", req.Cutscene.Id), zap.Error(err))
		return &pb.BaseResponse{Code: 500, Message: "Internal Error"}, nil
	}
	return &pb.BaseResponse{Code: 0, Message: "msg_success"}, nil
}
