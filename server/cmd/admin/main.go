package main

import (
	"context"
	"fmt"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"go.uber.org/zap"
	"github.com/prometheus/client_golang/prometheus/promhttp"
	"server/internal/admin"
	"server/pkg/config"
	"server/pkg/logger"
)

func main() {
	cfg, err := config.Load()
	if err != nil {
		fmt.Printf("Failed to load config: %v\n", err)
		os.Exit(1)
	}

	log := logger.Init(cfg)
	defer logger.Sync()

	h, err := admin.NewHandler(cfg, log)
	if err != nil {
		log.Fatal("Failed to init admin handler", zap.Error(err))
	}

	log.Info("Starting Admin/Web-API Server", 
		zap.String("env", cfg.App.Env),
	)

	cdnDir := "./cdn_data"
	_ = os.MkdirAll(cdnDir, 0755)

	fs := http.FileServer(http.Dir(cdnDir))
	http.Handle("/cdn/", http.StripPrefix("/cdn/", fs))

	http.HandleFunc("/api/login", h.Login)
	http.HandleFunc("/api/zones", h.GetZones)
	http.HandleFunc("/api/notices", h.GetActiveAnnouncements)
	http.HandleFunc("/api/profile", h.GetProfile)
	http.HandleFunc("/api/claim-daily", h.ClaimDaily)
	http.HandleFunc("/user/dashboard", h.UserDashboard)
	http.HandleFunc("/api/user/redeem", h.UserRedeemGiftCode)
	http.HandleFunc("/api/user/recharge", h.UserRecharge)

	http.HandleFunc("/api/gm/ping", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusOK)
		w.Write([]byte(`{"status":"ok"}`))
	})
	http.HandleFunc("/api/gm/zones", h.GMGetAllZones)
	http.HandleFunc("/api/gm/users/list", h.GMGetUsersList)
	http.HandleFunc("/api/gm/user", h.GMGetUserInfo)
	http.HandleFunc("/api/gm/inventory", h.GMGetUserInventory)
	http.HandleFunc("/api/gm/inventory/add", h.GMAddUserItem)
	http.HandleFunc("/api/gm/inventory/remove", h.GMRemoveUserItem)
	http.HandleFunc("/api/gm/item_configs", h.GMGetAllItemConfigs)
	http.HandleFunc("/api/gm/item_configs/save", h.GMSaveItemConfig)
	http.HandleFunc("/api/gm/item_configs/delete", h.GMDeleteItemConfig)
	http.HandleFunc("/api/gm/buildings/sync", h.GMSyncBuildings)
	http.HandleFunc("/api/gm/stages", h.GMGetAllStageConfigs)
	http.HandleFunc("/api/gm/stages/sync", h.GMSyncStageConfigs)
	http.HandleFunc("/api/gm/traits", h.GMGetAllTraitConfigs)
	http.HandleFunc("/api/gm/traits/sync", h.GMSyncTraitConfigs)
	http.HandleFunc("/api/gm/heroes", h.GMGetAllHeroTemplates)
	http.HandleFunc("/api/gm/heroes/sync", h.GMSyncHeroTemplates)

	http.HandleFunc("/api/gm/effect_configs", h.GMGetAllEffectConfigs)
	http.HandleFunc("/api/gm/effect_configs/save", h.GMSaveEffectConfig)
	http.HandleFunc("/api/gm/effect_configs/delete", h.GMDeleteEffectConfig)

	http.HandleFunc("/api/gm/feature_configs", h.GMGetAllFeatureConfigs)
	http.HandleFunc("/api/gm/feature_configs/save", h.GMSaveFeatureConfig)
	http.HandleFunc("/api/gm/feature_configs/delete", h.GMDeleteFeatureConfig)

	http.HandleFunc("/api/gm/mission_templates", h.GMGetAllMissionTemplates)
	http.HandleFunc("/api/gm/mission_templates/save", h.GMSaveMissionTemplate)
	http.HandleFunc("/api/gm/mission_templates/delete", h.GMDeleteMissionTemplate)

	http.HandleFunc("/api/gm/notices", h.GMGetAnnouncements)
	http.HandleFunc("/api/gm/notices/save", h.GMSaveAnnouncement)
	http.HandleFunc("/api/gm/notices/delete", h.GMDeleteAnnouncement)

	http.Handle("/metrics", promhttp.Handler())

	http.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
		w.Write([]byte("Admin/API Server OK"))
	})

	port := os.Getenv("ADMIN_PORT")
	if port == "" {
		port = "8080"
	}
	addr := ":" + port

	log.Info("Admin/API Server đang chạy", zap.String("addr", addr))
	
	srv := &http.Server{
		Addr:    addr,
		Handler: nil, // DefaultServeMux
	}

	go func() {
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Fatal("Server error", zap.Error(err))
		}
	}()

	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	log.Info("Shutting down Admin Server...")

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	if err := srv.Shutdown(ctx); err != nil {
		log.Fatal("Server forced to shutdown", zap.Error(err))
	}

	log.Info("Admin Server stopped")
}
