package admin

import (
	_ "embed"
	"encoding/json"
	"fmt"
	"html/template"
	"net/http"
	"strconv"
	"strings"

	"server/pkg/config"
	"server/pkg/jwtutil"

	"github.com/jmoiron/sqlx"
	_ "github.com/lib/pq"
	"go.uber.org/zap"
)

//go:embed user_dashboard.html
var userDashboardHTML string

var userDashboardTempl = template.Must(template.New("user_dashboard").Parse(userDashboardHTML))


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
	token, err := jwtManager.Generate(1, "test@seamou.studio")
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
		http.Error(w, "Thiếu Token xác thực. Vui lòng đăng nhập từ game.", http.StatusUnauthorized)
		return
	}

	jwtManager := jwtutil.New(h.cfg.JWT.Secret, h.cfg.JWT.ExpireHours)
	claims, err := jwtManager.Verify(token)
	if err != nil {
		http.Error(w, "Phiên làm việc hết hạn hoặc không hợp lệ. Vui lòng thử lại.", http.StatusUnauthorized)
		return
	}

	zoneStr := r.URL.Query().Get("zone_id")
	zoneID, _ := strconv.Atoi(zoneStr)
	if zoneID <= 0 {
		zoneID = 1
	}

	player, err := h.svc.GetPlayerInfoByUserID(claims.UserID, zoneID)
	hasCharacter := "true"
	nickname := "Chưa tạo"
	level := 0
	gold := int64(0)
	diamond := int64(0)

	if err != nil {
		hasCharacter = "false"
	} else {
		nickname = player.Nickname
		level = player.Level
		gold = player.Gold
		diamond = player.Diamond
	}

	zones, errZones := h.svc.GetAllZones()
	var zonesList []map[string]interface{}
	if errZones == nil {
		for _, z := range zones {
			zonesList = append(zonesList, map[string]interface{}{
				"ID":   z.ID,
				"Name": z.Name,
			})
		}
	}

	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	data := map[string]interface{}{
		"Nickname":     nickname,
		"Level":        level,
		"Gold":         gold,
		"Diamond":      diamond,
		"Token":        token,
		"ZoneID":       zoneID,
		"HasCharacter": hasCharacter,
		"Zones":        zonesList,
	}
	if err := userDashboardTempl.Execute(w, data); err != nil {
		h.log.Error("Failed to execute user dashboard template", zap.Error(err))
		http.Error(w, "Internal Server Error", http.StatusInternalServerError)
	}
}

func (h *AdminHandler) UserRedeemGiftCode(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Access-Control-Allow-Origin", "*")
	w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization")
	
	if r.Method == http.MethodOptions {
		w.WriteHeader(http.StatusOK)
		return
	}

	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	token := r.URL.Query().Get("token")
	if token == "" {
		authHeader := r.Header.Get("Authorization")
		if strings.HasPrefix(authHeader, "Bearer ") {
			token = strings.TrimPrefix(authHeader, "Bearer ")
		}
	}

	if token == "" {
		writeJSON(w, http.StatusUnauthorized, map[string]interface{}{"code": 401, "message": "Thiếu token xác thực"})
		return
	}

	jwtManager := jwtutil.New(h.cfg.JWT.Secret, h.cfg.JWT.ExpireHours)
	claims, err := jwtManager.Verify(token)
	if err != nil {
		writeJSON(w, http.StatusUnauthorized, map[string]interface{}{"code": 401, "message": "Phiên làm việc hết hạn"})
		return
	}

	var req struct {
		Code string `json:"code"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]interface{}{"code": 400, "message": "Định dạng yêu cầu không hợp lệ"})
		return
	}

	code := strings.TrimSpace(req.Code)
	if code == "" {
		writeJSON(w, http.StatusBadRequest, map[string]interface{}{"code": 400, "message": "Vui lòng điền mã GiftCode"})
		return
	}

	zoneStr := r.URL.Query().Get("zone_id")
	zoneID, _ := strconv.Atoi(zoneStr)
	if zoneID <= 0 {
		zoneID = 1
	}

	player, err := h.svc.GetPlayerInfoByUserID(claims.UserID, zoneID)
	if err != nil {
		writeJSON(w, http.StatusNotFound, map[string]interface{}{"code": 404, "message": "Không tìm thấy nhân vật của tài khoản này"})
		return
	}

	rewardDesc, err := h.svc.RedeemGiftCode(zoneID, player.ID, code)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]interface{}{"code": 400, "message": err.Error()})
		return
	}

	updatedPlayer, _ := h.svc.GetPlayerInfoByUserID(claims.UserID, zoneID)

	writeJSON(w, http.StatusOK, map[string]interface{}{
		"code":    0,
		"message": fmt.Sprintf("Đổi mã thành công! Bạn nhận được: %s", rewardDesc),
		"gold":    updatedPlayer.Gold,
		"diamond": updatedPlayer.Diamond,
	})
}

func (h *AdminHandler) UserRecharge(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Access-Control-Allow-Origin", "*")
	w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization")

	if r.Method == http.MethodOptions {
		w.WriteHeader(http.StatusOK)
		return
	}

	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	token := r.URL.Query().Get("token")
	if token == "" {
		authHeader := r.Header.Get("Authorization")
		if strings.HasPrefix(authHeader, "Bearer ") {
			token = strings.TrimPrefix(authHeader, "Bearer ")
		}
	}

	if token == "" {
		writeJSON(w, http.StatusUnauthorized, map[string]interface{}{"code": 401, "message": "Thiếu token xác thực"})
		return
	}

	jwtManager := jwtutil.New(h.cfg.JWT.Secret, h.cfg.JWT.ExpireHours)
	claims, err := jwtManager.Verify(token)
	if err != nil {
		writeJSON(w, http.StatusUnauthorized, map[string]interface{}{"code": 401, "message": "Phiên làm việc hết hạn"})
		return
	}

	var req struct {
		Amount int64 `json:"amount"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]interface{}{"code": 400, "message": "Định dạng yêu cầu không hợp lệ"})
		return
	}

	if req.Amount < 1000 {
		writeJSON(w, http.StatusBadRequest, map[string]interface{}{"code": 400, "message": "Số tiền nạp tối thiểu là 1,000đ"})
		return
	}

	zoneStr := r.URL.Query().Get("zone_id")
	zoneID, _ := strconv.Atoi(zoneStr)
	if zoneID <= 0 {
		zoneID = 1
	}

	player, err := h.svc.GetPlayerInfoByUserID(claims.UserID, zoneID)
	if err != nil {
		writeJSON(w, http.StatusNotFound, map[string]interface{}{"code": 404, "message": "Không tìm thấy nhân vật của tài khoản này"})
		return
	}

	newGold, newDiamond, err := h.svc.RechargePlayer(zoneID, player.ID, req.Amount)
	if err != nil {
		writeJSON(w, http.StatusInternalServerError, map[string]interface{}{"code": 500, "message": err.Error()})
		return
	}

	diamondReward := req.Amount / 1000
	goldReward := req.Amount * 10

	writeJSON(w, http.StatusOK, map[string]interface{}{
		"code":    0,
		"message": fmt.Sprintf("Nạp tiền thành công! Bạn nhận được +%d Vàng và +%d Xu.", goldReward, diamondReward),
		"gold":    newGold,
		"diamond": newDiamond,
	})
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

	// Dynamic host replacement if client connects using a remote IP/host
	reqHost := r.Host
	if idx := strings.Index(reqHost, ":"); idx != -1 {
		reqHost = reqHost[:idx]
	}
	if reqHost != "" && reqHost != "localhost" && reqHost != "127.0.0.1" && reqHost != "::1" {
		for i := range data.Zones {
			if data.Zones[i].Host == "localhost" || data.Zones[i].Host == "127.0.0.1" {
				data.Zones[i].Host = reqHost
			}
		}
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

	zoneStr := r.URL.Query().Get("zone_id")
	zoneID, err := strconv.Atoi(zoneStr)
	if err != nil || zoneID <= 0 {
		zoneID = 1 // default
	}

	info, err := h.svc.GetUserInfo(zoneID, userID)
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
	zoneStr := r.URL.Query().Get("zone_id")

	page, _ := strconv.Atoi(pageStr)
	if page < 1 {
		page = 1
	}

	limit, _ := strconv.Atoi(limitStr)
	if limit < 1 {
		limit = 20
	}

	zoneID, err := strconv.Atoi(zoneStr)
	if err != nil || zoneID <= 0 {
		zoneID = 1 // default
	}

	offset := (page - 1) * limit

	resp, err := h.svc.GetUsersPaginated(zoneID, limit, offset, search)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}
	resp.Page = page

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(resp)
}

func (h *AdminHandler) GMCleanupOrphanedPlayers(w http.ResponseWriter, r *http.Request) {
	zoneStr := r.URL.Query().Get("zone_id")
	zoneID, err := strconv.Atoi(zoneStr)
	if err != nil || zoneID <= 0 {
		zoneID = 1 // default
	}

	rowsDeleted, err := h.svc.CleanupOrphanedPlayers(zoneID)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"status":       "success",
		"rows_deleted": rowsDeleted,
	})
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

func (h *AdminHandler) GMAddHero(w http.ResponseWriter, r *http.Request) {
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

	var req struct {
		HeroCode string   `json:"hero_code"`
		Traits   []string `json:"traits"`
	}
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid Body", http.StatusBadRequest)
		return
	}

	if len(req.Traits) > 0 {
		err = h.svc.GMAddHeroWithTraits(zoneID, userID, req.HeroCode, req.Traits)
	} else {
		err = h.svc.GMAddHero(zoneID, userID, req.HeroCode)
	}
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
}

func (h *AdminHandler) GMCreateGiftCode(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Access-Control-Allow-Origin", "*")
	w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization")
	if r.Method == http.MethodOptions {
		w.WriteHeader(http.StatusOK)
		return
	}
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req struct {
		Code          string `json:"code"`
		RewardGold    int64  `json:"reward_gold"`
		RewardDiamond int64  `json:"reward_diamond"`
		RewardItems   string `json:"reward_items"`
		MaxUses       int    `json:"max_uses"`
	}

	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	code := strings.TrimSpace(req.Code)
	if code == "" {
		http.Error(w, "Mã code không được để trống", http.StatusBadRequest)
		return
	}

	if req.MaxUses <= 0 {
		req.MaxUses = 1
	}

	err := h.svc.CreateGiftCode(code, req.RewardGold, req.RewardDiamond, req.RewardItems, req.MaxUses)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	w.Write([]byte(`{"message":"Tạo GiftCode thành công"}`))
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

func (h *AdminHandler) GMGetAllFeatureConfigs(w http.ResponseWriter, r *http.Request) {
	configs, err := h.svc.GetAllFeatureConfigs()
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(configs)
}

func (h *AdminHandler) GMSaveFeatureConfig(w http.ResponseWriter, r *http.Request) {
	var req FeatureConfigData
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid Body", http.StatusBadRequest)
		return
	}

	if req.FeatureCode == "" {
		http.Error(w, "feature_code cannot be empty", http.StatusBadRequest)
		return
	}

	err := h.svc.SaveFeatureConfig(req)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
}

func (h *AdminHandler) GMDeleteFeatureConfig(w http.ResponseWriter, r *http.Request) {
	featureCode := r.URL.Query().Get("code")
	if featureCode == "" {
		http.Error(w, "Invalid Feature Code", http.StatusBadRequest)
		return
	}

	err := h.svc.DeleteFeatureConfig(featureCode)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
}

func (h *AdminHandler) GMGetAllMissionTemplates(w http.ResponseWriter, r *http.Request) {
	configs, err := h.svc.GetAllMissionTemplates()
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(configs)
}

func (h *AdminHandler) GMSaveMissionTemplate(w http.ResponseWriter, r *http.Request) {
	var req MissionTemplateData
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid Body", http.StatusBadRequest)
		return
	}

	if req.MissionID <= 0 {
		http.Error(w, "mission_id must be greater than 0", http.StatusBadRequest)
		return
	}

	err := h.svc.SaveMissionTemplate(req)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
}

func (h *AdminHandler) GMDeleteMissionTemplate(w http.ResponseWriter, r *http.Request) {
	missionIDStr := r.URL.Query().Get("id")
	var missionID int
	if _, err := fmt.Sscanf(missionIDStr, "%d", &missionID); err != nil {
		http.Error(w, "Invalid Mission ID", http.StatusBadRequest)
		return
	}

	err := h.svc.DeleteMissionTemplate(int32(missionID))
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
}

type SyncBuildingReq struct {
	Code     string `json:"code"`
	Name     string `json:"name"`
	MaxLevel int    `json:"max_level"`
	Desc     string `json:"desc"`
}

func (h *AdminHandler) GMSyncBuildings(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req []SyncBuildingReq
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, fmt.Sprintf("invalid request body: %v", err), http.StatusBadRequest)
		return
	}

	err := h.svc.SyncBuildings(req)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(map[string]string{"message": "Sync buildings successfully!"})
}

func (h *AdminHandler) GMGetAllStageConfigs(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	stages, err := h.svc.GetAllStageConfigs()
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(stages)
}

func (h *AdminHandler) GMSyncStageConfigs(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req []SyncStageReq
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, fmt.Sprintf("invalid request body: %v", err), http.StatusBadRequest)
		return
	}

	err := h.svc.SyncStageConfigs(req)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(map[string]string{"message": "Sync stages successfully!"})
}

func (h *AdminHandler) GMGetAllTraitConfigs(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	traits, err := h.svc.GetAllTraitConfigs()
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(traits)
}

func (h *AdminHandler) GMSyncTraitConfigs(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req []SyncTraitReq
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, fmt.Sprintf("invalid request body: %v", err), http.StatusBadRequest)
		return
	}

	err := h.svc.SyncTraitConfigs(req)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(map[string]string{"message": "Sync traits successfully!"})
}

func (h *AdminHandler) GMGetAllHeroTemplates(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	heroes, err := h.svc.GetAllHeroTemplates()
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(heroes)
}

func (h *AdminHandler) GMSyncHeroTemplates(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req []HeroTemplateDB
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, fmt.Sprintf("invalid request body: %v", err), http.StatusBadRequest)
		return
	}

	err := h.svc.SyncHeroTemplates(req)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(map[string]string{"message": "Sync hero templates successfully!"})
}

func (h *AdminHandler) GMGetAllShopItems(w http.ResponseWriter, r *http.Request) {
	configs, err := h.svc.GetAllShopItems()
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(configs)
}

func (h *AdminHandler) GMSaveShopItem(w http.ResponseWriter, r *http.Request) {
	var req ShopItemData
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "Invalid Body", http.StatusBadRequest)
		return
	}

	if req.ShopItemID == "" {
		http.Error(w, "shop_item_id cannot be empty", http.StatusBadRequest)
		return
	}

	err := h.svc.SaveShopItem(req)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
}

func (h *AdminHandler) GMDeleteShopItem(w http.ResponseWriter, r *http.Request) {
	shopItemID := r.URL.Query().Get("id")
	if shopItemID == "" {
		http.Error(w, "Invalid Shop Item ID", http.StatusBadRequest)
		return
	}

	err := h.svc.DeleteShopItem(shopItemID)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
}




