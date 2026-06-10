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

	player, err := h.svc.GetPlayerInfoByUserID(claims.UserID)
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

	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	w.Write([]byte(`
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Sect Origin - Cổng Tu Luyện</title>
    <link href="https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;600;700&display=swap" rel="stylesheet">
    <style>
        :root {
            --bg-color: #0b0c10;
            --container-bg: rgba(31, 40, 51, 0.65);
            --accent-color: #66fcf1;
            --accent-hover: #45f3e7;
            --text-color: #c5c6c7;
            --heading-color: #ffffff;
            --gold-color: #f39c12;
            --qi-color: #9b59b6;
            --border-radius: 16px;
        }

        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            font-family: 'Outfit', sans-serif;
        }

        body {
            background: radial-gradient(circle at center, #1f2833 0%, #0b0c10 100%);
            color: var(--text-color);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            padding: 20px;
        }

        .dashboard-container {
            width: 100%;
            max-width: 500px;
            background: var(--container-bg);
            border: 1px solid rgba(102, 252, 241, 0.25);
            backdrop-filter: blur(12px);
            border-radius: var(--border-radius);
            padding: 30px;
            box-shadow: 0 8px 32px 0 rgba(0, 0, 0, 0.5);
            transition: transform 0.3s ease;
        }

        .dashboard-container:hover {
            transform: translateY(-2px);
            border-color: rgba(102, 252, 241, 0.5);
        }

        h1 {
            color: var(--heading-color);
            text-align: center;
            font-size: 24px;
            font-weight: 700;
            margin-bottom: 25px;
            letter-spacing: 1px;
            text-shadow: 0 0 10px rgba(102, 252, 241, 0.3);
        }

        .profile-card {
            background: rgba(11, 12, 16, 0.8);
            border-radius: 12px;
            padding: 20px;
            margin-bottom: 25px;
            border: 1px solid rgba(255, 255, 255, 0.05);
        }

        .profile-row {
            display: flex;
            justify-content: space-between;
            margin-bottom: 10px;
            font-size: 15px;
        }

        .profile-row:last-child {
            margin-bottom: 0;
        }

        .label {
            color: #858688;
        }

        .value {
            font-weight: 600;
            color: var(--heading-color);
        }

        .value.accent {
            color: var(--accent-color);
        }

        .value.gold {
            color: var(--gold-color);
        }

        .value.qi {
            color: var(--qi-color);
        }

        .tabs {
            display: flex;
            margin-bottom: 20px;
            border-bottom: 1px solid rgba(255, 255, 255, 0.1);
        }

        .tab-btn {
            flex: 1;
            background: none;
            border: none;
            color: #858688;
            padding: 12px;
            font-size: 15px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s ease;
            position: relative;
        }

        .tab-btn.active {
            color: var(--accent-color);
        }

        .tab-btn.active::after {
            content: '';
            position: absolute;
            bottom: 0;
            left: 0;
            width: 100%;
            height: 2px;
            background: var(--accent-color);
            box-shadow: 0 0 8px var(--accent-color);
        }

        .tab-content {
            display: none;
        }

        .tab-content.active {
            display: block;
        }

        .form-group {
            margin-bottom: 20px;
        }

        label {
            display: block;
            margin-bottom: 8px;
            font-size: 14px;
            color: #858688;
        }

        .input-field {
            width: 100%;
            padding: 14px;
            background: rgba(11, 12, 16, 0.9);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 8px;
            color: var(--heading-color);
            font-size: 15px;
            transition: border-color 0.2s;
        }

        .input-field:focus {
            outline: none;
            border-color: var(--accent-color);
        }

        .btn-submit {
            width: 100%;
            padding: 14px;
            background: var(--accent-color);
            border: none;
            border-radius: 8px;
            color: #0b0c10;
            font-size: 16px;
            font-weight: 700;
            cursor: pointer;
            transition: all 0.2s;
            box-shadow: 0 0 15px rgba(102, 252, 241, 0.2);
        }

        .btn-submit:hover {
            background: var(--accent-hover);
            box-shadow: 0 0 20px rgba(102, 252, 241, 0.4);
            transform: translateY(-1px);
        }

        .btn-submit:active {
            transform: translateY(0);
        }

        .btn-submit:disabled {
            background: #4a5568;
            color: #a0aec0;
            cursor: not-allowed;
            box-shadow: none;
        }

        .recharge-grid {
            display: grid;
            grid-template-columns: repeat(2, 1fr);
            gap: 12px;
            margin-bottom: 20px;
        }

        .recharge-card {
            background: rgba(11, 12, 16, 0.6);
            border: 1px solid rgba(255, 255, 255, 0.05);
            border-radius: 10px;
            padding: 15px;
            text-align: center;
            cursor: pointer;
            transition: all 0.2s;
        }

        .recharge-card:hover {
            border-color: rgba(102, 252, 241, 0.3);
            background: rgba(11, 12, 16, 0.9);
        }

        .recharge-card.selected {
            border-color: var(--accent-color);
            background: rgba(102, 252, 241, 0.05);
            box-shadow: 0 0 10px rgba(102, 252, 241, 0.1);
        }

        .recharge-amount {
            font-size: 16px;
            font-weight: 700;
            color: var(--heading-color);
            margin-bottom: 5px;
        }

        .recharge-reward {
            font-size: 12px;
            color: var(--accent-color);
        }

        #result-box {
            margin-top: 15px;
            padding: 12px;
            border-radius: 8px;
            font-size: 14px;
            display: none;
            text-align: center;
        }

        .result-success {
            background: rgba(46, 204, 113, 0.15);
            color: #2ecc71;
            border: 1px solid rgba(46, 204, 113, 0.3);
        }

        .result-error {
            background: rgba(231, 76, 60, 0.15);
            color: #e74c3c;
            border: 1px solid rgba(231, 76, 60, 0.3);
        }
    </style>
</head>
<body>
    <div class="dashboard-container">
        <h1>CỔNG TU LUYỆN</h1>
        
        <div class="profile-card">
            <div class="profile-row">
                <span class="label">Tên Nhân Vật:</span>
                <span id="player-nickname" class="value accent">` + nickname + `</span>
            </div>
            <div class="profile-row">
                <span class="label">Cấp Độ:</span>
                <span id="player-level" class="value">` + fmt.Sprintf("%d", level) + `</span>
            </div>
            <div class="profile-row">
                <span class="label">Vàng Tích Lũy:</span>
                <span id="player-gold" class="value gold">` + fmt.Sprintf("%d", gold) + ` Vàng</span>
            </div>
            <div class="profile-row">
                <span class="label">Linh Lực Qi (Diamond):</span>
                <span id="player-diamond" class="value qi">` + fmt.Sprintf("%d", diamond) + ` Qi</span>
            </div>
        </div>

        <div class="tabs">
            <button class="tab-btn active" onclick="switchTab('redeem')">Đổi GiftCode</button>
            <button class="tab-btn" onclick="switchTab('recharge')">Nạp Linh Thạch</button>
        </div>

        <!-- Redeem Tab -->
        <div id="tab-redeem" class="tab-content active">
            <div class="form-group">
                <label for="giftcode-input">Nhập mã GiftCode:</label>
                <input type="text" id="giftcode-input" class="input-field" placeholder="Ví dụ: SECT666" style="text-transform: uppercase;">
            </div>
            <button id="btn-redeem" class="btn-submit" onclick="submitRedeem()">XÁC NHẬN ĐỔI MÃ</button>
        </div>

        <!-- Recharge Tab -->
        <div id="tab-recharge" class="tab-content">
            <label>Chọn mốc nạp tệ:</label>
            <div class="recharge-grid">
                <div class="recharge-card" onclick="selectRecharge(this, 10000)">
                    <div class="recharge-amount">10.000đ</div>
                    <div class="recharge-reward">+10 Qi / +100k Vàng</div>
                </div>
                <div class="recharge-card" onclick="selectRecharge(this, 50000)">
                    <div class="recharge-amount">50.000đ</div>
                    <div class="recharge-reward">+50 Qi / +500k Vàng</div>
                </div>
                <div class="recharge-card" onclick="selectRecharge(this, 100000)">
                    <div class="recharge-amount">100.000đ</div>
                    <div class="recharge-reward">+100 Qi / +1M Vàng</div>
                </div>
                <div class="recharge-card" onclick="selectRecharge(this, 200000)">
                    <div class="recharge-amount">200.000đ</div>
                    <div class="recharge-reward">+200 Qi / +2M Vàng</div>
                </div>
            </div>
            <button id="btn-recharge" class="btn-submit" onclick="submitRecharge()" disabled>XÁC NHẬN NẠP TIỀN</button>
        </div>

        <div id="result-box"></div>
    </div>

    <script>
        const token = "` + token + `";
        const hasChar = ` + hasCharacter + `;
        let selectedAmount = 0;

        if (!hasChar) {
            document.getElementById("btn-redeem").disabled = true;
            document.getElementById("btn-recharge").disabled = true;
            showResult("Bạn chưa tạo nhân vật trong game. Vui lòng vào game tạo nhân vật trước!", false);
        }

        function switchTab(tab) {
            document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(content => content.classList.remove('active'));
            
            event.currentTarget.classList.add('active');
            document.getElementById('tab-' + tab).classList.add('active');
            
            // clear result box
            const resBox = document.getElementById("result-box");
            resBox.style.display = "none";
        }

        function selectRecharge(element, amount) {
            if (!hasChar) return;
            document.querySelectorAll('.recharge-card').forEach(card => card.classList.remove('selected'));
            element.classList.add('selected');
            selectedAmount = amount;
            document.getElementById("btn-recharge").disabled = false;
        }

        function showResult(message, isSuccess) {
            const resBox = document.getElementById("result-box");
            resBox.className = isSuccess ? "result-success" : "result-error";
            resBox.innerText = message;
            resBox.style.display = "block";
        }

        async function submitRedeem() {
            const input = document.getElementById("giftcode-input");
            const code = input.value.trim();
            if (!code) {
                showResult("Vui lòng nhập mã GiftCode!", false);
                return;
            }

            const btn = document.getElementById("btn-redeem");
            btn.disabled = true;

            try {
                const response = await fetch("/api/user/redeem?token=" + encodeURIComponent(token), {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ code: code })
                });
                const res = await response.json();
                if (res.code === 0) {
                    showResult(res.message, true);
                    input.value = "";
                    updateStats(res.gold, res.diamond);
                } else {
                    showResult(res.message || "Đổi mã thất bại", false);
                }
            } catch (err) {
                showResult("Lỗi kết nối tới máy chủ", false);
            } finally {
                btn.disabled = false;
            }
        }

        async function submitRecharge() {
            if (selectedAmount <= 0) return;

            const btn = document.getElementById("btn-recharge");
            btn.disabled = true;

            try {
                const response = await fetch("/api/user/recharge?token=" + encodeURIComponent(token), {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ amount: selectedAmount })
                });
                const res = await response.json();
                if (res.code === 0) {
                    showResult(res.message, true);
                    updateStats(res.gold, res.diamond);
                } else {
                    showResult(res.message || "Nạp tiền thất bại", false);
                }
            } catch (err) {
                showResult("Lỗi kết nối tới máy chủ", false);
            } finally {
                btn.disabled = false;
            }
        }

        function updateStats(gold, diamond) {
            document.getElementById("player-gold").innerText = gold + " Vàng";
            document.getElementById("player-diamond").innerText = diamond + " Qi";
        }
    </script>
</body>
</html>
	`))
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

	player, err := h.svc.GetPlayerInfoByUserID(claims.UserID)
	if err != nil {
		writeJSON(w, http.StatusNotFound, map[string]interface{}{"code": 404, "message": "Không tìm thấy nhân vật của tài khoản này"})
		return
	}

	rewardDesc, err := h.svc.RedeemGiftCode(player.ID, code)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, map[string]interface{}{"code": 400, "message": err.Error()})
		return
	}

	updatedPlayer, _ := h.svc.GetPlayerInfoByUserID(claims.UserID)

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

	player, err := h.svc.GetPlayerInfoByUserID(claims.UserID)
	if err != nil {
		writeJSON(w, http.StatusNotFound, map[string]interface{}{"code": 404, "message": "Không tìm thấy nhân vật của tài khoản này"})
		return
	}

	newGold, newDiamond, err := h.svc.RechargePlayer(player.ID, req.Amount)
	if err != nil {
		writeJSON(w, http.StatusInternalServerError, map[string]interface{}{"code": 500, "message": err.Error()})
		return
	}

	diamondReward := req.Amount / 1000
	goldReward := req.Amount * 10

	writeJSON(w, http.StatusOK, map[string]interface{}{
		"code":    0,
		"message": fmt.Sprintf("Nạp tiền thành công! Bạn nhận được +%d Vàng và +%d Qi.", goldReward, diamondReward),
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




