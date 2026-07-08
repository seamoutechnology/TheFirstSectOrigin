package base

type Building struct {
	ID              string           `json:"id"`
	X               int              `json:"x"`
	Y               int              `json:"y"`
	Level           int              `json:"level"`
	UpgradeBranches map[string]int   `json:"upgrade_branches"` // Lưu cấp độ của từng nhánh (VD: "Speed": 2, "Quality": 1)
	Storage         map[string]int64 `json:"storage"`          // Lưu số lượng vật phẩm đang chờ thu hoạch (VD: "GoldOre": 50)
	LastHarvestTime int64            `json:"last_harvest_time"`// Thời điểm thu hoạch cuối cùng (Unix timestamp)
}

type BaseLayout struct {
	GridWidth  int        `json:"grid_width"`
	GridHeight int        `json:"grid_height"`
	Buildings  []Building `json:"buildings"`
}

type ShareCodeRecord struct {
	PINCode   string     `json:"pin_code"`
	Layout    BaseLayout `json:"layout"`
	OwnerID   string     `json:"owner_id"`
	CreatedAt int64      `json:"created_at"`
}
