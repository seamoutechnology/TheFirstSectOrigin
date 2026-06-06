package admin

import (
	"encoding/json"
	"fmt"
	"net/http"
	"strconv"
	"strings"

	"server/pkg/config"
	"server/pkg/jwtutil"

	"github.com/jmoiron/sqlx"
	_ "github.com/lib/pq"
	"go.uber.org/zap"
)

func writeJSON(w http.ResponseWriter, status int, v interface{}) {
	data, err := json.Marshal(v)
	if err != nil {
		http.Error(w, fmt.Sprintf("JSON marshal error: %v", err), http.StatusInternalServerError)
		return
	}
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.Header().Set("Content-Length", fmt.Sprintf("%d", len(data)))
	w.WriteHeader(status)
	w.Write(data)
}

type AdminHandler struct {
	svc Service
	cfg *config.Config
	log *zap.Logger
}

func NewHandler(cfg *config.Config, log *zap.Logger) (*AdminHandler, error) {
	db, err := sqlx.Connect("postgres", cfg.Postgres.DSN)
	if err != nil {
		return nil, err
	}

	gameDB, err := sqlx.Connect("postgres", cfg.GameDB.DSN)
	if err != nil {
		return nil, err
	}

	repo := NewRepository(db, gameDB, cfg)
	svc := NewService(repo)

	return &AdminHandler{
		svc: svc,
		cfg: cfg,
		log: log,
	}, nil
}

func (h *AdminHandler) Login(w http.ResponseWriter, r *http.Request) {
	jwtManager := jwtutil.New(h.cfg.JWT.Secret, h.cfg.JWT.ExpireHours)
	token, err := jwtManager.Generate(1, "test@sectorigin.com")
	if err != nil {
		http.Error(w, "Lỗi tạo token", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]string{
		"token":   token,
		"message": "Đăng nhập thành công (Token Thật)",
	})
}

func (h *AdminHandler) GetProfile(w http.ResponseWriter, r *http.Request) {
	resp := map[string]interface{}{
		"username":  "Đạo hữu ẩn danh",
		"level":     99,
		"sect_name": "Thanh Vân Môn",
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(resp)
}

func (h *AdminHandler) ClaimDaily(w http.ResponseWriter, r *http.Request) {
	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(map[string]string{"message": "ClaimDaily API - Sẵn sàng để code logic"})
}

func (h *AdminHandler) UserDashboard(w http.ResponseWriter, r *http.Request) {
	token := r.URL.Query().Get("token")
	if token == "" {
		http.Error(w, "Thiếu Token xác thực", http.StatusUnauthorized)
		return
	}

	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	w.Write([]byte(`
		<html>
			<head><title>Sect Origin - User Dashboard</title></head>
			<body style="background: #1a1a1a; color: white; font-family: sans-serif; padding: 50px;">
				<h1>Chào mừng Đạo Hữu đến với Sect Origin</h1>
				<p>Token của bạn: ` + token + `</p>
				<hr/>
				<button style="padding: 10px 20px; background: #e67e22; border: none; color: white; cursor: pointer;">
					NHẬP REDEEM CODE
				</button>
				<p>Hệ thống đang được nâng cấp thêm các tính năng Nạp thẻ và Lịch sử giao dịch...</p>
			</body>
		</html>
	`))
}


func (h *AdminHandler) GetActiveAnnouncements(w http.ResponseWriter, r *http.Request) {
	list, err := h.svc.GetActiveAnnouncements()
	if err != nil {
		h.log.Error("Lỗi truy vấn DB", zap.Error(err))
		http.Error(w, "Lỗi truy vấn", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(list)
}

func (h *AdminHandler) GMGetAnnouncements(w http.ResponseWriter, r *http.Request) {
	list, err := h.svc.GetAllAnnouncements()
	if err != nil {
		h.log.Error("Lỗi truy vấn DB", zap.Error(err))
		http.Error(w, "Lỗi truy vấn", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(list)
}

func (h *AdminHandler) GMSaveAnnouncement(w http.ResponseWriter, r *http.Request) {
	var req AnnouncementRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		h.log.Error("Lỗi parse JSON", zap.Error(err))
		http.Error(w, err.Error(), http.StatusBadRequest)
		return
	}

	var err error
	if req.ID > 0 {
		err = h.svc.UpdateAnnouncement(req)
	} else {
		err = h.svc.CreateAnnouncement(req)
	}

	if err != nil {
		h.log.Error("Lỗi DB", zap.Error(err))
		http.Error(w, "Lỗi lưu vào database", http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(map[string]string{"message": "Đã lưu thông báo thành công"})
}

func (h *AdminHandler) GMDeleteAnnouncement(w http.ResponseWriter, r *http.Request) {
	var req struct {
		ID int `json:"id"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		h.log.Error("Lỗi parse JSON", zap.Error(err))
		http.Error(w, err.Error(), http.StatusBadRequest)
		return
	}

	if err := h.svc.DeleteAnnouncement(req.ID); err != nil {
		h.log.Error("Lỗi DB", zap.Error(err))
		http.Error(w, "Lỗi xoá database", http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(map[string]string{"message": "Đã xoá thành công"})
}

func (h *AdminHandler) GetZones(w http.ResponseWriter, r *http.Request) {
	var req ZoneReq
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		req.Type = "meta"
	}

	if req.Type == "meta" {
		meta, err := h.svc.GetZoneMeta()
		if err != nil {
			http.Error(w, "Lỗi Server", http.StatusInternalServerError)
			return
		}
		writeJSON(w, http.StatusOK, meta)
		return
	}

	var userID int64 = 0
	authHeader := r.Header.Get("Authorization")
	if strings.HasPrefix(authHeader, "Bearer ") {
		tokenStr := strings.TrimPrefix(authHeader, "Bearer ")
		jwtManager := jwtutil.New(h.cfg.JWT.Secret, h.cfg.JWT.ExpireHours)
		if claims, err := jwtManager.Verify(tokenStr); err == nil {
			userID = claims.UserID
		}
	}

	data, err := h.svc.GetZoneData(req.TabID, userID)
	if err != nil {
		http.Error(w, "Lỗi truy vấn DB", http.StatusInternalServerError)
		return
	}

	writeJSON(w, http.StatusOK, data)
}


func (h *AdminHandler) GMGetUserInfo(w http.ResponseWriter, r *http.Request) {
	idStr := r.URL.Query().Get("id")
	userID, err := strconv.ParseInt(idStr, 10, 64)
	if err != nil {
		http.Error(w, "Invalid User ID", http.StatusBadRequest)
		return
	}

	info, err := h.svc.GetUserInfo(userID)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(info)
}

func (h *AdminHandler) GMGetUsersList(w http.ResponseWriter, r *http.Request) {
	pageStr := r.URL.Query().Get("page")
	limitStr := r.URL.Query().Get("limit")
	search := r.URL.Query().Get("search")

	page, _ := strconv.Atoi(pageStr)
	if page < 1 {
		page = 1
	}

	limit, _ := strconv.Atoi(limitStr)
	if limit < 1 {
		limit = 20
	}

	offset := (page - 1) * limit

	resp, err := h.svc.GetUsersPaginated(limit, offset, search)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}
	resp.Page = page

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(resp)
}

func (h *AdminHandler) GMGetAllZones(w http.ResponseWriter, r *http.Request) {
	zones, err := h.svc.GetAllZones()
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(zones)
}

func (h *AdminHandler) GMGetUserInventory(w http.ResponseWriter, r *http.Request) {
	idStr := r.URL.Query().Get("id")
	userID, err := strconv.ParseInt(idStr, 10, 64)
	if err != nil {
		http.Error(w, "Invalid User ID", http.StatusBadRequest)
		return
	}

	zoneStr := r.URL.Query().Get("zone_id")
	zoneID, err := strconv.Atoi(zoneStr)
	if err != nil || zoneID <= 0 {
		zoneID = 1 // default
	}

	items, err := h.svc.GetUserInventory(zoneID, userID)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(items)
}

func (h *AdminHandler) GMAddUserItem(w http.ResponseWriter, r *http.Request) {
	idStr := r.URL.Query().Get("id")
	userID, err := strconv.ParseInt(idStr, 10, 64)
	if err != nil {
		http.Error(w, "Invalid User ID", http.StatusBadRequest)
		return
	}

	zoneStr := r.URL.Query().Get("zone_id")
	zoneID, err := strconv.Atoi(zoneStr)
	if err != nil || zoneID <= 0 {
		zoneID = 1 // default
	}

	var req AddItemRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid Body", http.StatusBadRequest)
		return
	}

	err = h.svc.AddUserItem(zoneID, userID, req.ItemCode, req.Quantity)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
}

func (h *AdminHandler) GMRemoveUserItem(w http.ResponseWriter, r *http.Request) {
	itemStr := r.URL.Query().Get("item_id")
	itemID, err := strconv.ParseInt(itemStr, 10, 64)
	if err != nil {
		http.Error(w, "Invalid Item ID", http.StatusBadRequest)
		return
	}
	
	zoneStr := r.URL.Query().Get("zone_id")
	zoneID, err := strconv.Atoi(zoneStr)
	if err != nil || zoneID <= 0 {
		zoneID = 1 // default
	}

	err = h.svc.RemoveUserItem(zoneID, itemID)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
}


func (h *AdminHandler) GMGetAllItemConfigs(w http.ResponseWriter, r *http.Request) {
	configs, err := h.svc.GetAllItemConfigs()
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(configs)
}

func (h *AdminHandler) GMSaveItemConfig(w http.ResponseWriter, r *http.Request) {
	var req ItemConfigData
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid Body", http.StatusBadRequest)
		return
	}

	if req.ItemCode == "" {
		http.Error(w, "item_code cannot be empty", http.StatusBadRequest)
		return
	}

	err := h.svc.SaveItemConfig(req)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
}

func (h *AdminHandler) GMDeleteItemConfig(w http.ResponseWriter, r *http.Request) {
	itemCode := r.URL.Query().Get("code")
	if itemCode == "" {
		http.Error(w, "Invalid Item Code", http.StatusBadRequest)
		return
	}

	err := h.svc.DeleteItemConfig(itemCode)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
}


func (h *AdminHandler) GMGetAllEffectConfigs(w http.ResponseWriter, r *http.Request) {
	configs, err := h.svc.GetAllEffectConfigs()
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(configs)
}

func (h *AdminHandler) GMSaveEffectConfig(w http.ResponseWriter, r *http.Request) {
	var req EffectConfigData
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid Body", http.StatusBadRequest)
		return
	}

	if req.EffectCode == "" {
		http.Error(w, "effect_code cannot be empty", http.StatusBadRequest)
		return
	}

	err := h.svc.SaveEffectConfig(req)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
}

func (h *AdminHandler) GMDeleteEffectConfig(w http.ResponseWriter, r *http.Request) {
	effectCode := r.URL.Query().Get("code")
	if effectCode == "" {
		http.Error(w, "Invalid Effect Code", http.StatusBadRequest)
		return
	}

	err := h.svc.DeleteEffectConfig(effectCode)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
}
