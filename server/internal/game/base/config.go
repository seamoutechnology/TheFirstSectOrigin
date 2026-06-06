package base

type EffectType string

const (
	EffectResourceGen EffectType = "RESOURCE_GEN"
	EffectStatBoost   EffectType = "STAT_BOOST"
	EffectUnlockTech  EffectType = "UNLOCK_TECH"
)

type BuildingConfig struct {
	ID           string
	SizeX        int
	SizeY        int
	EffectType   EffectType
	BaseInterval int64  // Chu kỳ sản xuất mặc định (giây)
	BaseYield    int64  // Số lượng sản phẩm tạo ra mỗi chu kỳ
	MaxCapacity  int64  // Sức chứa tối đa của Kho chờ
	MainProduct  string // Vật phẩm sinh ra (Ví dụ: "Gold", "Wood")
	RareProduct  string // Vật phẩm hiếm có tỷ lệ rơi ra (Ví dụ: "Diamond")
}

const (
	BranchSpeed   = "Speed"
	BranchQuality = "Quality"
	BranchYield   = "Yield"
)

var Database = map[string]BuildingConfig{
	"MAIN_HALL": {
		ID: "MAIN_HALL", SizeX: 4, SizeY: 4, EffectType: EffectUnlockTech,
	},
	"GOLD_MINE": {
		ID: "GOLD_MINE", SizeX: 2, SizeY: 2, EffectType: EffectResourceGen,
		BaseInterval: 3600, BaseYield: 100, MaxCapacity: 500, MainProduct: "Gold", RareProduct: "Diamond",
	},
	"TRAINING_HQ": {
		ID: "TRAINING_HQ", SizeX: 3, SizeY: 3, EffectType: EffectStatBoost,
	},
}
