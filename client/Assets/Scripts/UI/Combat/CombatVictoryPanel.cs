using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using GameClient.Core;
using GameClient.UI.Core;
using GameClient.Managers;
using GameClient.Network.Pb;
using GameClient.Gameplay.World;
using GameClient.Network;

namespace GameClient.UI.Combat
{
    public class CombatVictoryPanel : BaseUIPanel
    {
        [Header("Reward UI Details")]
        [SerializeField] private TMP_Text txtExpReward;
        [SerializeField] private TMP_Text txtLinhThachReward;
        [SerializeField] private Transform rewardsGridParent;
        [SerializeField] private GameObject rewardItemPrefab; // Optional prefab to instantiate for item rewards

        [Header("Buttons")]
        [SerializeField] private Button btnExit;
        [SerializeField] private Button btnRaid;

        protected override void OnInit()
        {
            base.OnInit();
            btnExit.onClick.AddListener(OnExitClicked);

            if (btnRaid == null)
            {
                btnRaid = transform.Find("btnRaid")?.GetComponent<Button>();
                if (btnRaid == null)
                {
                    // Fallback to find any button named with raid/sweep/quat
                    foreach (var b in GetComponentsInChildren<Button>(true))
                    {
                        string nameLower = b.name.ToLower();
                        if (nameLower.Contains("raid") || nameLower.Contains("sweep") || nameLower.Contains("quat"))
                        {
                            btnRaid = b;
                            break;
                        }
                    }
                }
            }

            if (btnRaid != null)
            {
                btnRaid.onClick.AddListener(OnRaidClicked);
            }
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);

            int exp = 0;
            int linhThach = 0;

            if (data is ValidatePvEResultResponse resp)
            {
                exp = resp.RewardExp;
                linhThach = resp.RewardLinhThach;
            }
            else if (data is CombatResultPanel.LocalResultData localData)
            {
                exp = localData.RewardExp;
                linhThach = localData.RewardLinhThach;
            }

            txtExpReward.text = $"+{exp} EXP";
            txtLinhThachReward.text = $"+{linhThach} Linh Thạch";

            // Render items from current stage drops
            PopulateStageItemDrops();
        }

        private void PopulateStageItemDrops()
        {
            if (rewardsGridParent == null) return;

            // Clear old icons
            foreach (Transform child in rewardsGridParent)
            {
                Destroy(child.gameObject);
            }

            var stage = CombatStartData.CurrentStage;
            if (stage == null || stage.rewards == null || stage.rewards.Count == 0)
            {
                rewardsGridParent.gameObject.SetActive(false);
                return;
            }

            rewardsGridParent.gameObject.SetActive(true);
            foreach (var reward in stage.rewards)
            {
                if (rewardItemPrefab != null)
                {
                    GameObject itemGo = Instantiate(rewardItemPrefab, rewardsGridParent);
                    LoadItemVisualsAsync(itemGo, reward.itemId, reward.amount);
                }
                else
                {
                    // Fallback to dynamic Text GameObject
                    GameObject txtGo = new GameObject("RewardItemTxt");
                    txtGo.transform.SetParent(rewardsGridParent, false);
                    var text = txtGo.AddComponent<TextMeshProUGUI>();
                    text.text = $"• {reward.itemId} x{reward.amount}";
                    text.fontSize = 20;
                    text.color = Color.yellow;
                    text.alignment = TextAlignmentOptions.Center;
                    
                    // Cố gắng dịch tên kể cả khi dùng fallback text
                    TranslateFallbackText(text, reward.itemId, reward.amount);
                }
            }
        }

        private async void TranslateFallbackText(TextMeshProUGUI text, string itemCode, int amount)
        {
            var config = ItemDataManager.Instance.GetItemConfig(itemCode);
            string displayName = itemCode;
            if (config != null)
            {
                string localizedName = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.ITEM_EQUIPMENT, config.NameKey);
                if (!string.IsNullOrEmpty(localizedName) && !localizedName.StartsWith("["))
                {
                    displayName = localizedName;
                }
            }
            if (text != null)
            {
                text.text = $"• {displayName} x{amount}";
            }
        }

        private async void LoadItemVisualsAsync(GameObject itemGo, string itemCode, int amount)
        {
            var config = ItemDataManager.Instance.GetItemConfig(itemCode);
            string displayName = itemCode;
            if (config != null)
            {
                string localizedName = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.ITEM_EQUIPMENT, config.NameKey);
                if (!string.IsNullOrEmpty(localizedName) && !localizedName.StartsWith("["))
                {
                    displayName = localizedName;
                }
            }

            // 1. Tìm và gán Text (Tên & Số lượng)
            var txts = itemGo.GetComponentsInChildren<TMP_Text>(true);
            if (txts.Length > 0)
            {
                if (txts.Length == 1)
                {
                    txts[0].text = $"{displayName}\nx{amount}";
                }
                else
                {
                    TMP_Text txtName = null;
                    TMP_Text txtQty = null;
                    foreach (var t in txts)
                    {
                        string nameLower = t.name.ToLower();
                        if (nameLower.Contains("name") || nameLower.Contains("title")) txtName = t;
                        else if (nameLower.Contains("qty") || nameLower.Contains("quantity") || nameLower.Contains("count") || nameLower.Contains("amount")) txtQty = t;
                    }
                    if (txtName != null) txtName.text = displayName;
                    if (txtQty != null) txtQty.text = $"x{amount}";

                    if (txtName == null && txtQty == null)
                    {
                        txts[0].text = displayName;
                        if (txts.Length > 1) txts[1].text = $"x{amount}";
                    }
                }
            }

            // 2. Tìm và gán Icon từ Addressable
            Image imgIcon = null;
            foreach (var img in itemGo.GetComponentsInChildren<Image>(true))
            {
                string nameLower = img.gameObject.name.ToLower();
                if (nameLower.Contains("icon") || nameLower.Contains("avatar") || nameLower.Contains("thumb"))
                {
                    imgIcon = img;
                    break;
                }
            }
            // Nếu không tìm thấy bằng tên, lấy đại component Image đầu tiên mà không phải Background
            if (imgIcon == null)
            {
                var imgs = itemGo.GetComponentsInChildren<Image>(true);
                if (imgs.Length > 1) imgIcon = imgs[1]; // Thường index 0 là background panel của item slot
                else if (imgs.Length == 1) imgIcon = imgs[0];
            }

            if (imgIcon != null)
            {
                Sprite sprite = null;
                string iconKey = "";
                if (config != null && !string.IsNullOrEmpty(config.Icon))
                {
                    iconKey = config.Icon;
                }
                else
                {
                    if (itemCode == "00000" || itemCode == "coin") iconKey = "coin_icon";
                    else if (itemCode == "00002" || itemCode == "stone" || itemCode == "stone_1") iconKey = "stone_1_icon";
                    else if (itemCode == "00003" || itemCode == "wood" || itemCode == "wood_1") iconKey = "wood_1_icon";
                    else if (itemCode == "gold") iconKey = "gold_icon";
                    else iconKey = itemCode + "_icon";
                }

                try
                {
                    sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(iconKey);
                }
                catch (System.Exception)
                {
                    try
                    {
                        sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(itemCode);
                    }
                    catch (System.Exception) {}
                }

                if (sprite != null)
                {
                    imgIcon.sprite = sprite;
                    imgIcon.preserveAspect = true;
                }
            }
        }

        private void OnExitClicked()
        {
            Hide();
            if (MapManager.Instance != null)
            {
                _ = MapManager.Instance.LoadMapAsync(MapType.LocalBase);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("LocalBase");
            }
        }

        private async void OnRaidClicked()
        {
            var stage = CombatStartData.CurrentStage;
            if (stage == null)
            {
                Debug.LogError("[CombatVictoryPanel] Không tìm thấy dữ liệu ải hiện tại.");
                return;
            }

            if (GameManager.Instance.CurrentPlayer == null)
            {
                Debug.LogError("[CombatVictoryPanel] Không tìm thấy thông tin người chơi.");
                return;
            }

            // Kiểm tra Năng lượng
            if (GameManager.Instance.CurrentPlayer.Stamina < stage.staminaCost)
            {
                UIManager.Instance.ShowMessage("Thiếu Năng Lượng", $"Không đủ Năng lượng! Cần {stage.staminaCost} Năng lượng để càn quét.");
                return;
            }

            if (btnRaid != null) btnRaid.interactable = false;

            try
            {
                var req = new ValidatePvEResultRequest
                {
                    EnemyId = stage.stageId,
                    IsVictory = true,
                    PlayerPower = (int)GameManager.Instance.CurrentPlayer.Power,
                    EnemyPower = stage.recommendPower,
                };

                var res = await NetworkManager.Instance.GatewayClient.ValidatePvEResultAsync(req, NetworkManager.DefaultCallOptions());
                if (res != null && res.IsValid)
                {
                    // Tải lại profile để cập nhật Năng lượng và tài nguyên trên UI
                    var profileRes = await NetworkManager.Instance.GatewayClient.GetPlayerProfileAsync(new GetPlayerProfileRequest(), NetworkManager.DefaultCallOptions());
                    if (profileRes != null && profileRes.Base != null && profileRes.Base.Code == 0 && profileRes.Profile != null)
                    {
                        GameManager.Instance.SetPlayer(profileRes.Profile);
                    }

                    string rewardMsg = $"Nhận +{res.RewardExp} EXP, +{res.RewardLinhThach} Linh Thạch";
                    if (stage.rewards != null && stage.rewards.Count > 0)
                    {
                        rewardMsg += "\n\nVật phẩm nhận được:";
                        foreach (var reward in stage.rewards)
                        {
                            rewardMsg += $"\n• {reward.itemId} x{reward.amount}";
                        }
                    }

                    UIManager.Instance.ShowMessage("Càn Quét Thành Công", rewardMsg);
                }
                else
                {
                    string errorMsg = res?.Base?.Message ?? "Lỗi không xác định từ máy chủ.";
                    UIManager.Instance.ShowMessage("Càn Quét Thất Bại", errorMsg);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CombatVictoryPanel] Exception during Raid: {ex.Message}");
                UIManager.Instance.ShowMessage("Lỗi Càn Quét", $"Không thể kết nối máy chủ hoặc đã xảy ra lỗi: {ex.Message}");
            }
            finally
            {
                if (btnRaid != null) btnRaid.interactable = true;
            }
        }
    }
}
