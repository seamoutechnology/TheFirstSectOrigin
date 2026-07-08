package main

import (
	"encoding/json"
	"io"
	"net/http"
	"strings"

	"go.uber.org/zap"
	"server/internal/gateway/proxy"
	pb "server/pkg/pb"
)

func RegisterCutsceneHTTPHandler(mux *http.ServeMux, worldClient *proxy.WorldClient, log *zap.Logger) {
	mux.HandleFunc("/api/v1/cutscenes/", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Access-Control-Allow-Origin", "*")
		w.Header().Set("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
		w.Header().Set("Access-Control-Allow-Headers", "Content-Type")

		if r.Method == "OPTIONS" {
			w.WriteHeader(http.StatusOK)
			return
		}

		path := strings.TrimPrefix(r.URL.Path, "/api/v1/cutscenes")
		path = strings.TrimPrefix(path, "/") // empty or ID

		if r.Method == http.MethodGet {
			if path == "" {
				resp, err := worldClient.ListCutscenes(r.Context(), &pb.EmptyRequest{})
				if err != nil {
					log.Error("ListCutscenes failed", zap.Error(err))
					http.Error(w, err.Error(), http.StatusInternalServerError)
					return
				}
				
				w.Header().Set("Content-Type", "application/json")
				json.NewEncoder(w).Encode(map[string]interface{}{
					"ids": resp.Ids,
				})
				return
			} else {
				id := strings.TrimSuffix(path, ".json")
				resp, err := worldClient.GetCutscene(r.Context(), &pb.GetCutsceneRequest{Id: id})
				if err != nil {
					if strings.Contains(err.Error(), "record not found") {
						http.Error(w, "Cutscene not found", http.StatusNotFound)
						return
					}
					log.Error("GetCutscene failed", zap.String("id", id), zap.Error(err))
					http.Error(w, err.Error(), http.StatusInternalServerError)
					return
				}
				
				w.Header().Set("Content-Type", "application/json")
				w.Write([]byte(resp.JsonContent))
				return
			}
		}

		if r.Method == http.MethodPost {
			id := strings.TrimSuffix(path, ".json")
			if id == "" {
				http.Error(w, "Missing ID", http.StatusBadRequest)
				return
			}
			bodyBytes, err := io.ReadAll(r.Body)
			if err != nil {
				http.Error(w, "Read body failed", http.StatusBadRequest)
				return
			}
			
			_, err = worldClient.SaveCutscene(r.Context(), &pb.SaveCutsceneRequest{
				Cutscene: &pb.CutsceneData{
					Id:          id,
					JsonContent: string(bodyBytes),
				},
			})
			if err != nil {
				log.Error("SaveCutscene failed", zap.String("id", id), zap.Error(err))
				http.Error(w, err.Error(), http.StatusInternalServerError)
				return
			}
			
			w.WriteHeader(http.StatusOK)
			w.Write([]byte(`{"status":"ok"}`))
			return
		}

		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
	})
}
