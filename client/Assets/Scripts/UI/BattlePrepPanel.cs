using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using GameClient.Gameplay.World;
using GameClient.Managers;
using GameClient.UI.Core;
using GameClient.Network.Api;
using GameClient.Network.Pb;

namespace GameClient.UI
{
    public class BattlePrepPanel : BaseUIPanel
    {
        [Header("Stage Info UI")]
        [SerializeField] private TMP_Text txtStageName;
        [SerializeField] private TMP_Text txtStageDesc;
        [SerializeField] private TMP_Text txtRecommendPower;
        [SerializeField] private TMP_Text txtStamina;

        [Header("Enemy / Boss Preview")]
        [SerializeField] private Transform enemyListRoot;
        [SerializeField] private GameObject enemySlotPrefab;

        [Header("Formation Slots (3x3 Grid - 9 Slots)")]
        [Tooltip("Gán 9 ô vị trí tương ứng trong đội hình 3x3 (Index 0 to 8)")]
        [SerializeField] private Button[] formationSlotButtons;
        [SerializeField] private TMP_Text[] formationSlotTexts;

        [Header("Owned Heroes List")]
        [SerializeField] private Transform ownedHeroesContainer;
        [SerializeField] private GameObject ownedHeroItemPrefab;

        [Header("Action Buttons")]
        [SerializeField] private Button btnStartCombat;
        [SerializeField] private Button btnSaveFormation;
        [SerializeField] private Button btnClose;

        private StageData _stageData;
        private Dictionary<int, long> _tempFormation = new Dictionary<int, long>(); // Position (0-8) -> PlayerHeroId
        private int _selectedSlotIndex = -1; // Fallback click selection

        protected override void OnStart()
        {
            base.OnStart();
            
            btnClose.onClick.AddListener(Hide);
            btnStartCombat.onClick.AddListener(OnStartCombatClicked);
            btnSaveFormation.onClick.AddListener(OnSaveFormationClicked);

            for (int i = 0; i < formationSlotButtons.Length; i++)
            {
                int index = i;
                formationSlotButtons[i].onClick.AddListener(() => SelectFormationSlot(index));
                
                // Add slot component dynamically if missing
                var slotComp = formationSlotButtons[index].gameObject.GetComponent<BattleFormationSlot>();
                if (slotComp == null)
                {
                    slotComp = formationSlotButtons[index].gameObject.AddComponent<BattleFormationSlot>();
                }
                slotComp.SlotIndex = index;
            }
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            if (data is StageData stage)
            {
                _stageData = stage;
                LoadStageInfo();
            }

            LoadCurrentFormation();
            RefreshFormationUI();
            RefreshOwnedHeroes();
        }

        public int GetFormationCount()
        {
            return _tempFormation.Count;
        }

        private void LoadStageInfo()
        {
            if (_stageData == null) return;

            string localizedName = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, _stageData.stageName);
            txtStageName.text = !string.IsNullOrEmpty(localizedName) ? localizedName : _stageData.stageName;

            string localizedDesc = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, _stageData.description);
            txtStageDesc.text = !string.IsNullOrEmpty(localizedDesc) ? localizedDesc : _stageData.description;

            txtRecommendPower.text = $"Lực chiến đề nghị: {_stageData.recommendPower}";
            txtStamina.text = $"-{_stageData.staminaCost} Thể lực";

            // Hiển thị danh sách kẻ địch / Boss
            foreach (Transform child in enemyListRoot)
            {
                Destroy(child.gameObject);
            }

            foreach (var monster in _stageData.enemiesConfig)
            {
                var slot = Instantiate(enemySlotPrefab, enemyListRoot);
                var text = slot.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    string monsterName = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, monster.name);
                    if (string.IsNullOrEmpty(monsterName)) monsterName = monster.name;
                    text.text = monsterName + (monster.isBoss ? " [BOSS]" : $" (Lv.{monster.level})");
                }
            }
        }

        private void LoadCurrentFormation()
        {
            _tempFormation.Clear();
            if (GameManager.Instance.Formation != null)
            {
                foreach (var slot in GameManager.Instance.Formation)
                {
                    if (slot.Position >= 0 && slot.Position < 9)
                    {
                        _tempFormation[slot.Position] = slot.PlayerHeroId;
                    }
                }
            }
        }

        private void RefreshFormationUI()
        {
            for (int i = 0; i < formationSlotButtons.Length; i++)
            {
                if (_tempFormation.TryGetValue(i, out long heroId) && heroId > 0)
                {
                    var hero = GameManager.Instance.PlayerHeroes.Find(h => h.Id == heroId);
                    if (hero != null)
                    {
                        formationSlotTexts[i].text = hero.Name;
                        
                        // Add drag capability to hero on board
                        var drag = formationSlotButtons[i].gameObject.GetComponent<BattleHeroDragItem>();
                        if (drag == null) drag = formationSlotButtons[i].gameObject.AddComponent<BattleHeroDragItem>();
                        drag.Setup(heroId, true, i, this);
                    }
                    else
                    {
                        formationSlotTexts[i].text = "Trống";
                        RemoveDragComponent(formationSlotButtons[i].gameObject);
                    }
                }
                else
                {
                    formationSlotTexts[i].text = "Trống";
                    RemoveDragComponent(formationSlotButtons[i].gameObject);
                }
            }
        }

        private void RemoveDragComponent(GameObject go)
        {
            var drag = go.GetComponent<BattleHeroDragItem>();
            if (drag != null)
            {
                Destroy(drag);
            }
        }

        private void RefreshOwnedHeroes()
        {
            foreach (Transform child in ownedHeroesContainer)
            {
                Destroy(child.gameObject);
            }

            var ownedHeroes = GameManager.Instance.PlayerHeroes;
            if (ownedHeroes == null) return;

            foreach (var hero in ownedHeroes)
            {
                // Check if this hero is already placed in formation
                bool isPlaced = _tempFormation.ContainsValue(hero.Id);

                var item = Instantiate(ownedHeroItemPrefab, ownedHeroesContainer);
                var text = item.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    text.text = $"[{hero.Rarity}] {hero.Name}\nLv.{hero.Level}";
                }

                var btn = item.GetComponent<Button>();
                if (btn != null)
                {
                    long hId = hero.Id;
                    btn.onClick.AddListener(() => AssignHeroToSelectedSlot(hId));
                }

                // Add drag component to cards in owned list
                var drag = item.GetComponent<BattleHeroDragItem>();
                if (drag == null) drag = item.AddComponent<BattleHeroDragItem>();
                drag.Setup(hero.Id, false, -1, this);

                // UI feedback if hero is already on board or dragging is locked
                var canvasGroup = item.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = item.AddComponent<CanvasGroup>();
                
                if (isPlaced)
                {
                    canvasGroup.alpha = 0.4f; // Semi-transparent to indicate placed
                }
                else if (GetFormationCount() >= 5)
                {
                    canvasGroup.alpha = 0.6f; // Indicate drag is locked
                }
                else
                {
                    canvasGroup.alpha = 1.0f;
                }
            }
        }

        private void SelectFormationSlot(int index)
        {
            _selectedSlotIndex = index;
            ToastManager.Instance.ShowNormalToast($"Đã chọn vị trí {index + 1}. Kéo hoặc nhấp tướng để xếp vào ô này.");
        }

        private void AssignHeroToSelectedSlot(long heroId)
        {
            if (_selectedSlotIndex == -1)
            {
                ToastManager.Instance.ShowNormalToast("Vui lòng chọn một ô vị trí đội hình trước!");
                return;
            }

            HandleHeroDroppedOnSlot(heroId, _selectedSlotIndex, false, -1);
        }

        public void HandleHeroDroppedOnSlot(long heroId, int targetSlotIndex, bool wasOnBoard, int originSlotIndex)
        {
            if (wasOnBoard)
            {
                // Dragged from one slot to another
                if (_tempFormation.TryGetValue(targetSlotIndex, out long existingHeroId))
                {
                    // Swap positions
                    _tempFormation[targetSlotIndex] = heroId;
                    _tempFormation[originSlotIndex] = existingHeroId;
                }
                else
                {
                    // Move to new empty position
                    _tempFormation.Remove(originSlotIndex);
                    _tempFormation[targetSlotIndex] = heroId;
                }
                ToastManager.Instance.ShowNormalToast("Đã hoán đổi vị trí!");
            }
            else
            {
                // Dragged from owned list to board
                // Check if hero is already placed somewhere else
                int currentPos = -1;
                foreach (var kv in _tempFormation)
                {
                    if (kv.Value == heroId)
                    {
                        currentPos = kv.Key;
                        break;
                    }
                }

                if (currentPos != -1)
                {
                    // Swap with existing target, or just move
                    if (_tempFormation.TryGetValue(targetSlotIndex, out long existingHeroId))
                    {
                        _tempFormation[targetSlotIndex] = heroId;
                        _tempFormation[currentPos] = existingHeroId;
                    }
                    else
                    {
                        _tempFormation.Remove(currentPos);
                        _tempFormation[targetSlotIndex] = heroId;
                    }
                }
                else
                {
                    // Overwrite if target has a hero
                    if (_tempFormation.ContainsKey(targetSlotIndex))
                    {
                        _tempFormation[targetSlotIndex] = heroId;
                    }
                    else
                    {
                        // Add new to board if under 5 limit
                        if (GetFormationCount() < 5)
                        {
                            _tempFormation[targetSlotIndex] = heroId;
                        }
                        else
                        {
                            ToastManager.Instance.ShowNormalToast("Đội hình tối đa 5 tướng!");
                            return;
                        }
                    }
                }
                ToastManager.Instance.ShowNormalToast("Đã xếp tướng thành công!");
            }

            RefreshFormationUI();
            RefreshOwnedHeroes();
        }

        public void RemoveHeroFromFormation(long heroId)
        {
            int slotPos = -1;
            foreach (var kv in _tempFormation)
            {
                if (kv.Value == heroId)
                {
                    slotPos = kv.Key;
                    break;
                }
            }

            if (slotPos != -1)
            {
                _tempFormation.Remove(slotPos);
                ToastManager.Instance.ShowNormalToast("Đã gỡ tướng khỏi đội hình!");
                RefreshFormationUI();
                RefreshOwnedHeroes();
            }
        }

        private async void OnSaveFormationClicked()
        {
            var slotsToSend = new List<(int, long)>();
            foreach (var kv in _tempFormation)
            {
                slotsToSend.Add((kv.Key, kv.Value));
            }

            try
            {
                var response = await DiscipleApi.SetFormationAsync(slotsToSend);
                if (response != null)
                {
                    ToastManager.Instance.ShowNormalToast("Đã lưu đội hình lên máy chủ thành công!");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BattlePrepPanel] Lỗi lưu đội hình: {ex.Message}");
                ToastManager.Instance.ShowNormalToast("Không thể lưu đội hình!");
            }
        }

        private void OnStartCombatClicked()
        {
            if (_tempFormation.Count == 0)
            {
                ToastManager.Instance.ShowNormalToast("Đội hình trận chiến không được trống!");
                return;
            }

            // Đồng bộ đội hình hiện tại lên static data
            CombatStartData.CurrentStage = _stageData;
            CombatStartData.SelectedHeroIds = new List<long>(_tempFormation.Values);

            Hide();
            SceneTransitionManager.Instance.TransitionToScene(_stageData.combatSceneName);
        }
    }

    public static class CombatStartData
    {
        public static StageData CurrentStage;
        public static List<long> SelectedHeroIds;
    }
}
