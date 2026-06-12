using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using GameClient.Gameplay.World;
using GameClient.Managers;
using GameClient.UI.Core;
using GameClient.Network.Api;
using GameClient.Network.Pb;
using DG.Tweening;
using System.Linq;
using GameClient.Core;
using GameClient.Gameplay.Combat;

namespace GameClient.UI
{
    public class BattlePrepPanel : BaseUIPanel
    {
        [Header("Stage Info UI")]
        [SerializeField] private TMP_Text txtStageName;
        [SerializeField] private TMP_Text txtTeamPower; // Hiển thị tổng chiến lực của đội hình

        [Header("Enemy / Boss Preview")]
        [SerializeField] private Transform enemyListRoot;
        [SerializeField] private GameObject enemySlotPrefab;

        [Header("Formation Slots (3x3 Grid - 9 Slots)")]
        [SerializeField] private Transform formationGridRoot; // Ô chứa lưới trận hình (FormPlacement) để spawn vào
        [SerializeField] private GameObject formationSlotPrefab; // Prefab FormationItem chứa script UI_FormationSlotItem

        private UI_FormationSlotItem[] _slotItems = new UI_FormationSlotItem[9];

        [Header("Owned Heroes List")]
        [SerializeField] private Transform ownedHeroesContainer;
        [SerializeField] private GameObject ownedHeroItemPrefab;

        [Header("Action Buttons")]
        [SerializeField] private Button btnStartCombat;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnQuickDeploy; // Xếp đội nhanh
        [SerializeField] private Button btnAutoDeploy;  // Hạ trận nhanh (Auto-deploy)
        [SerializeField] private Button btnRule;        // Nút xem quy tắc (Rule)

        [Header("Sort Buttons")]
        [SerializeField] private Button btnSortPower;
        [SerializeField] private Button btnSortLevel;
        [SerializeField] private Button btnSortRarity;

        private enum HeroSortType
        {
            Power,
            Level,
            Rarity
        }
        private HeroSortType _currentSortType = HeroSortType.Power;

        private StageData _stageData;
        private Dictionary<int, long> _tempFormation = new Dictionary<int, long>(); // Position (0-8) -> PlayerHeroId
        private int _selectedSlotIndex = -1; // Fallback click selection

        private struct FormationPreset
        {
            public string Name;
            public HashSet<int> ActiveSlots;
        }

        private List<FormationPreset> _presets = new List<FormationPreset>()
        {
            new FormationPreset { Name = "Tự do", ActiveSlots = new HashSet<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 } },
            new FormationPreset { Name = "Hình chữ T", ActiveSlots = new HashSet<int> { 0, 1, 2, 4, 7 } },
            new FormationPreset { Name = "Hình chữ T Ngược", ActiveSlots = new HashSet<int> { 1, 4, 6, 7, 8 } },
            new FormationPreset { Name = "Hình chữ T Xoay", ActiveSlots = new HashSet<int> { 0, 3, 6, 4, 5 } },
            new FormationPreset { Name = "Hình chữ V", ActiveSlots = new HashSet<int> { 0, 2, 3, 5, 7 } },
            new FormationPreset { Name = "Hình chữ X", ActiveSlots = new HashSet<int> { 0, 2, 4, 6, 8 } },
            new FormationPreset { Name = "Hình chữ V Ngược", ActiveSlots = new HashSet<int> { 1, 3, 5, 6, 8 } },
            new FormationPreset { Name = "Hình chữ I :", ActiveSlots = new HashSet<int> { 0, 3, 6, 2, 8 } },
            new FormationPreset { Name = "Hình chữ : I", ActiveSlots = new HashSet<int> { 0, 6, 2, 5, 8 } }
        };

        private int _currentPresetIndex = 0;
        private Button btnToggleFormation;
        private TMP_Text txtToggleFormation;
        private int _blessedSlotIndex = -1;

        protected override void OnStart()
        {
            base.OnStart();
            
            // Xoá toàn bộ ô con cũ trong container nếu có
            if (formationGridRoot != null)
            {
                foreach (Transform child in formationGridRoot)
                {
                    Destroy(child.gameObject);
                }
            }

            // Sinh 9 ô đấu từ prefab
            for (int i = 0; i < 9; i++)
            {
                int index = i;
                if (formationSlotPrefab != null && formationGridRoot != null)
                {
                    var go = Instantiate(formationSlotPrefab, formationGridRoot);
                    var slotItem = go.GetComponent<UI_FormationSlotItem>();
                    if (slotItem == null) slotItem = go.AddComponent<UI_FormationSlotItem>();
                    
                    _slotItems[index] = slotItem;

                    slotItem.Button.onClick.RemoveAllListeners();
                    slotItem.Button.onClick.AddListener(() => SelectFormationSlot(index));

                    // Thêm component kéo thả động
                    var slotComp = go.GetComponent<BattleFormationSlot>();
                    if (slotComp == null) slotComp = go.AddComponent<BattleFormationSlot>();
                    slotComp.SlotIndex = index;
                }
            }

            btnClose.onClick.AddListener(Hide);
            btnStartCombat.onClick.AddListener(OnStartCombatClicked);

            if (btnQuickDeploy != null) btnQuickDeploy.onClick.AddListener(OnQuickDeployClicked);
            if (btnAutoDeploy != null) btnAutoDeploy.onClick.AddListener(OnAutoDeployClicked);
            if (btnRule != null) btnRule.onClick.AddListener(OnRuleClicked);

            if (btnSortPower != null) btnSortPower.onClick.AddListener(() => SetSortTypeAndRefresh(HeroSortType.Power));
            if (btnSortLevel != null) btnSortLevel.onClick.AddListener(() => SetSortTypeAndRefresh(HeroSortType.Level));
            if (btnSortRarity != null) btnSortRarity.onClick.AddListener(() => SetSortTypeAndRefresh(HeroSortType.Rarity));

            // Điều chỉnh chiều cao của Scroll View và bố cục Content để hiển thị trọn vẹn thẻ tướng 300px không bị cắt
            if (ownedHeroesContainer != null)
            {
                var viewport = ownedHeroesContainer.parent as RectTransform;
                if (viewport != null)
                {
                    var scrollView = viewport.parent as RectTransform;
                    if (scrollView != null)
                    {
                        // Mở rộng anchor chiều cao của Scroll View lên để có đủ không gian cho thẻ 300px
                        var anchorMax = scrollView.anchorMax;
                        anchorMax.y = 0.38f; // Tăng vùng phủ chiều cao từ 25% lên 38% màn hình
                        scrollView.anchorMax = anchorMax;

                        var anchorMin = scrollView.anchorMin;
                        anchorMin.y = 0.02f; // Neo sát cạnh dưới
                        scrollView.anchorMin = anchorMin;

                        var sizeDelta = scrollView.sizeDelta;
                        sizeDelta.y = 0f; // Reset bù trừ chiều cao để co giãn theo anchor
                        scrollView.sizeDelta = sizeDelta;

                        var anchoredPos = scrollView.anchoredPosition;
                        anchoredPos.y = 0f; // Reset vị trí Y
                        scrollView.anchoredPosition = anchoredPos;
                    }
                }

                // Cấu hình lại Content để căn giữa các thẻ và tăng chiều cao chứa vừa thẻ
                var contentRt = ownedHeroesContainer as RectTransform;
                if (contentRt != null)
                {
                    contentRt.anchoredPosition = Vector2.zero; // Reset vị trí Y
                    contentRt.sizeDelta = new Vector2(contentRt.sizeDelta.x, 320f); // Chiều cao 320px
                }

                var layoutGroup = ownedHeroesContainer.GetComponent<HorizontalLayoutGroup>();
                if (layoutGroup != null)
                {
                    layoutGroup.childAlignment = TextAnchor.MiddleCenter; // Căn giữa thẳng đứng
                }
            }
        }

        public override async void Setup(object data = null)
        {
            base.Setup(data);
            if (data is StageData stage)
            {
                _stageData = stage;
                LoadStageInfo();
            }

            // Đồng bộ danh sách tướng của người chơi từ máy chủ khi mở panel
            try
            {
                var response = await GameClient.Network.Api.DiscipleApi.GetHeroesAsync();
                if (response != null && response.Base != null && response.Base.Code == 0)
                {
                    GameManager.Instance.SetHeroes(response.Heroes);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BattlePrepPanel] Lỗi tải danh sách tướng từ máy chủ: {ex.Message}");
            }

            // Đảm bảo dữ liệu cấu hình tướng (HeroConfig) đã nạp xong từ Resources trước khi hiển thị
            await GameClient.Managers.HeroDataManager.Instance.LoadAllHeroesAsync();

            LoadCurrentFormation();

            // Chọn ngẫu nhiên ô cát tường phe ta lúc mở bảng chuẩn bị đấu
            var activeSlots = new List<int>(_presets[_currentPresetIndex].ActiveSlots);
            if (activeSlots.Count > 0)
            {
                _blessedSlotIndex = activeSlots[Random.Range(0, activeSlots.Count)];
            }
            else
            {
                _blessedSlotIndex = -1;
            }
            CombatStartData.BlessedSlotIndex = _blessedSlotIndex;

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

            if (enemyListRoot == null)
            {
                Debug.LogWarning("[BattlePrepPanel] enemyListRoot is not assigned in the inspector!");
                return;
            }

            // Đảm bảo enemyListRoot có HorizontalLayoutGroup để các ô quái không chồng lên nhau
            if (enemyListRoot.GetComponent<LayoutGroup>() == null)
            {
                var layout = enemyListRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 15f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            // Hiển thị danh sách kẻ địch / Boss
            foreach (Transform child in enemyListRoot)
            {
                Destroy(child.gameObject);
            }

            if (enemySlotPrefab == null)
            {
                Debug.LogWarning("[BattlePrepPanel] enemySlotPrefab is not assigned in the inspector!");
                return;
            }

            foreach (var monster in _stageData.enemiesConfig)
            {
                var slot = Instantiate(enemySlotPrefab, enemyListRoot);
                string monsterName = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, monster.name);
                if (string.IsNullOrEmpty(monsterName)) monsterName = monster.name;

                var slotItem = slot.GetComponent<UI_FormationSlotItem>();
                if (slotItem != null)
                {
                    // Tắt tương tác của nút bấm vì đây là quái vật phe địch chỉ hiển thị
                    slotItem.Button.interactable = false;

                    string statusText = monsterName + (monster.isBoss ? " [BOSS]" : $" (Lv.{monster.level})");
                    slotItem.SetStatusText(statusText);
                    slotItem.SetTextBgActive(true);

                    // Tải avatar quái vật tự động
                    SetEnemyAvatarOnSlot(slotItem, monster);
                }
                else
                {
                    // Fallback nếu dùng prefab text cũ
                    var text = slot.GetComponentInChildren<TMP_Text>();
                    if (text != null)
                    {
                        text.text = monsterName + (monster.isBoss ? " [BOSS]" : $" (Lv.{monster.level})");
                    }
                }
            }
        }

        private async void SetEnemyAvatarOnSlot(UI_FormationSlotItem slotItem, MonsterConfig monster)
        {
            // Thử tải sprite đại diện cho quái vật từ địa chỉ prefabAddress
            string address = !string.IsNullOrEmpty(monster.prefabAddress) ? monster.prefabAddress : "";
            
            // Nếu địa chỉ là của Prefab (GameObject) thì không thể tải trực tiếp thành Sprite.
            // Để tránh lỗi LogError từ ResourceManager, ta chuyển hướng sang avatar đại diện của quái vật.
            if (address.Contains("Prefab") || address.Contains("prefab"))
            {
                address = "avt_03_img"; // Dùng avatar mặc định cho quái vật
            }

            if (string.IsNullOrEmpty(address) || address.Contains(" ") || address == monster.name)
            {
                if (slotItem != null)
                {
                    slotItem.SetHeroVisual(null, false);
                }
                return;
            }

            try
            {
                Sprite sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(address);
                if (slotItem != null)
                {
                    slotItem.SetHeroVisual(sprite, sprite != null);
                }
            }
            catch (System.Exception)
            {
                if (slotItem != null)
                {
                    slotItem.SetHeroVisual(null, false);
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
            var activeSlots = _presets[_currentPresetIndex].ActiveSlots;
            for (int i = 0; i < 9; i++)
            {
                if (_slotItems[i] == null) continue;

                bool isActive = activeSlots.Contains(i);
                _slotItems[i].Button.interactable = isActive;

                if (!isActive)
                {
                    _slotItems[i].SetStatusText("");
                    _slotItems[i].SetHeroVisual(null, false);
                    _slotItems[i].SetTextBgActive(false);
                    RemoveDragComponent(_slotItems[i].gameObject);
                    continue;
                }

                if (_tempFormation.TryGetValue(i, out long heroId) && heroId > 0)
                {
                    var hero = GameManager.Instance.PlayerHeroes.Find(h => h.Id == heroId);
                    if (hero != null)
                    {
                        if (i == _blessedSlotIndex)
                        {
                            _slotItems[i].SetStatusText(LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_blessed_indicator", hero.Name));
                        }
                        else
                        {
                            _slotItems[i].SetStatusText(hero.Name);
                        }

                        _slotItems[i].SetTextBgActive(true);

                        // Load avatar dynamically
                        SetHeroAvatarOnSlot(i, hero);
                        
                        // Add drag capability to hero on board
                        var drag = _slotItems[i].gameObject.GetComponent<BattleHeroDragItem>();
                        if (drag == null) drag = _slotItems[i].gameObject.AddComponent<BattleHeroDragItem>();
                        drag.Setup(heroId, true, i, this);
                    }
                    else
                    {
                        _slotItems[i].SetStatusText("");
                        _slotItems[i].SetHeroVisual(null, false);
                        _slotItems[i].SetTextBgActive(false);
                        RemoveDragComponent(_slotItems[i].gameObject);
                    }
                }
                else
                {
                    _slotItems[i].SetStatusText("");
                    _slotItems[i].SetHeroVisual(null, false);
                    _slotItems[i].SetTextBgActive(false);
                    RemoveDragComponent(_slotItems[i].gameObject);
                }
            }

            UpdateBlessedSlotVisuals();
            UpdateTeamPower();
        }

        private async void SetHeroAvatarOnSlot(int slotIndex, Hero hero)
        {
            var config = HeroDataManager.Instance.GetHeroConfigByCodeOrName(hero.Name);
            if (config == null) config = HeroDataManager.Instance.GetHeroConfig(hero.Id);
            string address = (config != null && !string.IsNullOrEmpty(config.iconAddress)) ? config.iconAddress : "";

            if (string.IsNullOrEmpty(address) || address.Contains(" "))
            {
                if (_slotItems[slotIndex] != null)
                {
                    _slotItems[slotIndex].SetHeroVisual(null, false);
                }
                return;
            }

            try
            {
                Sprite sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(address);
                if (_slotItems[slotIndex] != null)
                {
                    _slotItems[slotIndex].SetHeroVisual(sprite, true);
                }
            }
            catch (System.Exception)
            {
                if (_slotItems[slotIndex] != null)
                {
                    _slotItems[slotIndex].SetHeroVisual(null, false);
                }
            }
        }

        private void UpdateTeamPower()
        {
            if (txtTeamPower == null) return;

            long totalPower = 0;
            foreach (var heroId in _tempFormation.Values)
            {
                var hero = GameManager.Instance.PlayerHeroes.Find(h => h.Id == heroId);
                if (hero != null)
                {
                    totalPower += hero.Power;
                }
            }

            string powerText = totalPower >= 1000 ? $"{totalPower / 1000f:F1}K" : totalPower.ToString();
            txtTeamPower.text = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_team_power", powerText);
        }

        private void UpdateBlessedSlotVisuals()
        {
            for (int i = 0; i < 9; i++)
            {
                if (_slotItems[i] == null) continue;
                var btn = _slotItems[i].Button;
                var glowTrans = btn.transform.Find("BlessedGlow");
                
                if (i == _blessedSlotIndex)
                {
                    if (glowTrans == null)
                    {
                        var glowGo = new GameObject("BlessedGlow", typeof(RectTransform), typeof(Image));
                        glowGo.transform.SetParent(btn.transform, false);
                        
                        var rect = glowGo.GetComponent<RectTransform>();
                        rect.anchorMin = Vector2.zero;
                        rect.anchorMax = Vector2.one;
                        rect.sizeDelta = Vector2.zero;
                        
                        var img = glowGo.GetComponent<Image>();
                        img.color = new Color(1f, 0.75f, 0f, 0.35f); // Màu vàng cam lấp lánh
                        img.DOComplete();
                        img.DOFade(0.1f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                    }
                }
                else
                {
                    if (glowTrans != null)
                    {
                        var img = glowTrans.GetComponent<Image>();
                        if (img != null) img.DOKill();
                        Destroy(glowTrans.gameObject);
                    }
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

        private void SetSortTypeAndRefresh(HeroSortType sortType)
        {
            _currentSortType = sortType;
            string key = "combat_prep_sort_power";
            if (sortType == HeroSortType.Level) key = "combat_prep_sort_level";
            else if (sortType == HeroSortType.Rarity) key = "combat_prep_sort_rarity";
            ToastManager.Instance.ShowBigToast(LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, key));
            RefreshOwnedHeroes();
        }

        private int GetRarityPriority(string rarity)
        {
            switch (rarity.ToUpper())
            {
                case "SSS": return 0;
                case "SS": return 1;
                case "S": return 2;
                case "A": return 3;
                case "B": return 4;
                case "C": return 5;
                default: return 6;
            }
        }

        private void RefreshOwnedHeroes()
        {
            foreach (Transform child in ownedHeroesContainer)
            {
                Destroy(child.gameObject);
            }

            var ownedHeroesList = GameManager.Instance.PlayerHeroes;
            if (ownedHeroesList == null) return;

            // Sắp xếp danh sách theo tiêu chí hiện tại
            var sortedHeroes = new List<Hero>(ownedHeroesList);
            if (_currentSortType == HeroSortType.Power)
            {
                sortedHeroes = sortedHeroes.OrderByDescending(h => h.Power).ToList();
            }
            else if (_currentSortType == HeroSortType.Level)
            {
                sortedHeroes = sortedHeroes.OrderByDescending(h => h.Level).ToList();
            }
            else if (_currentSortType == HeroSortType.Rarity)
            {
                sortedHeroes = sortedHeroes.OrderBy(h => GetRarityPriority(h.Rarity)).ThenByDescending(h => h.Power).ToList();
            }

            foreach (var hero in sortedHeroes)
            {
                // Check if this hero is already placed in formation
                bool isPlaced = _tempFormation.ContainsValue(hero.Id);

                var item = Instantiate(ownedHeroItemPrefab, ownedHeroesContainer);
                var heroItem = item.GetComponent<UI_PrepOwnedHeroItem>();
                if (heroItem != null)
                {
                    long hId = hero.Id;
                    heroItem.Setup(hero, isPlaced, () => AssignHeroToSelectedSlot(hId));
                }
                else
                {
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
                else if (GetFormationCount() >= 9)
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
            ToastManager.Instance.ShowBigToast(LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_select_slot", index + 1));
        }

        private void AssignHeroToSelectedSlot(long heroId)
        {
            if (_selectedSlotIndex == -1)
            {
                ToastManager.Instance.ShowBigToast(LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_select_slot_first"));
                return;
            }

            var activeSlots = _presets[_currentPresetIndex].ActiveSlots;
            if (!activeSlots.Contains(_selectedSlotIndex))
            {
                ToastManager.Instance.ShowBigToast(LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_slot_not_active"));
                return;
            }

            HandleHeroDroppedOnSlot(heroId, _selectedSlotIndex, false, -1);
        }

        public void HandleHeroDroppedOnSlot(long heroId, int targetSlotIndex, bool wasOnBoard, int originSlotIndex)
        {
            var activeSlots = _presets[_currentPresetIndex].ActiveSlots;
            if (!activeSlots.Contains(targetSlotIndex))
            {
                ToastManager.Instance.ShowBigToast(LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_target_not_active"));
                RefreshFormationUI();
                RefreshOwnedHeroes();
                return;
            }

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
                ToastManager.Instance.ShowBigToast(LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_swap_success"));
            }
            else
            {
                // Dragged from owned list to board
                // Ngăn chặn ra trận trùng lặp tướng cùng tên/loại
                var heroToPlace = GameManager.Instance.PlayerHeroes.Find(h => h.Id == heroId);
                if (heroToPlace != null)
                {
                    foreach (var kv in _tempFormation)
                    {
                        if (kv.Value != heroId)
                        {
                            var existingHero = GameManager.Instance.PlayerHeroes.Find(h => h.Id == kv.Value);
                            if (existingHero != null && existingHero.Name == heroToPlace.Name)
                            {
                                string localizedMsg = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_duplicate_hero");
                                if (string.IsNullOrEmpty(localizedMsg) || localizedMsg.StartsWith("["))
                                {
                                    localizedMsg = "Không thể ra trận trùng lặp tướng: {0}!";
                                }
                                ToastManager.Instance.ShowBigToast(string.Format(localizedMsg, heroToPlace.Name));
                                RefreshFormationUI();
                                RefreshOwnedHeroes();
                                return;
                            }
                        }
                    }
                }

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
                        // Add new to board if under 9 limit
                        if (GetFormationCount() < 9)
                        {
                            _tempFormation[targetSlotIndex] = heroId;
                        }
                        else
                        {
                            ToastManager.Instance.ShowBigToast(LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_max_heroes"));
                            return;
                        }
                    }
                }
                ToastManager.Instance.ShowBigToast(LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_place_success"));
            }

            RefreshFormationUI();
            RefreshOwnedHeroes();
        }

        private void OnToggleFormationClicked()
        {
            _currentPresetIndex = (_currentPresetIndex + 1) % _presets.Count;
            UpdateFormationButtonText();
            ApplyFormationPreset(_presets[_currentPresetIndex]);
        }

        private void UpdateFormationButtonText()
        {
            if (txtToggleFormation != null)
            {
                txtToggleFormation.text = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_formation_label", _presets[_currentPresetIndex].Name);
            }
        }

        private void ApplyFormationPreset(FormationPreset preset)
        {
            // Chọn lại ô cát tường ngẫu nhiên trong số các ô kích hoạt mới
            var activeSlots = new List<int>(preset.ActiveSlots);
            if (activeSlots.Count > 0)
            {
                _blessedSlotIndex = activeSlots[Random.Range(0, activeSlots.Count)];
            }
            else
            {
                _blessedSlotIndex = -1;
            }
            CombatStartData.BlessedSlotIndex = _blessedSlotIndex;

            // Find any heroes that are currently in slots that will become inactive
            List<long> displacedHeroes = new List<long>();
            List<int> slotsToRemove = new List<int>();
            
            foreach (var kv in _tempFormation)
            {
                if (!preset.ActiveSlots.Contains(kv.Key))
                {
                    displacedHeroes.Add(kv.Value);
                    slotsToRemove.Add(kv.Key);
                }
            }
            
            // Remove them from their inactive positions
            foreach (var slot in slotsToRemove)
            {
                _tempFormation.Remove(slot);
            }
            
            // Try to place the displaced heroes into available active slots in the new preset
            foreach (var heroId in displacedHeroes)
            {
                int targetSlot = -1;
                foreach (var activeSlot in preset.ActiveSlots)
                {
                    if (!_tempFormation.ContainsKey(activeSlot))
                    {
                        targetSlot = activeSlot;
                        break;
                    }
                }
                
                if (targetSlot != -1)
                {
                    _tempFormation[targetSlot] = heroId;
                }
                else
                {
                    string heroName = "Tướng";
                    var hero = GameManager.Instance.PlayerHeroes.Find(h => h.Id == heroId);
                    if (hero != null) heroName = hero.Name;
                    ToastManager.Instance.ShowBigToast(LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_evict_hero", heroName));
                }
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
                ToastManager.Instance.ShowBigToast(LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_evict_hero", ""));
                RefreshFormationUI();
                RefreshOwnedHeroes();
            }
        }

        private void OnQuickDeployClicked()
        {
            LoadCurrentFormation();
            ToastManager.Instance.ShowBigToast(LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_quick_deploy_success"));
            RefreshFormationUI();
            RefreshOwnedHeroes();
        }

        private void OnAutoDeployClicked()
        {
            _tempFormation.Clear();
            var activeSlots = _presets[_currentPresetIndex].ActiveSlots;
            
            var ownedList = GameManager.Instance.PlayerHeroes;
            if (ownedList == null || ownedList.Count == 0) return;

            var sortedHeroes = new List<Hero>(ownedList);
            if (_currentSortType == HeroSortType.Power)
            {
                sortedHeroes = sortedHeroes.OrderByDescending(h => h.Power).ToList();
            }
            else if (_currentSortType == HeroSortType.Level)
            {
                sortedHeroes = sortedHeroes.OrderByDescending(h => h.Level).ToList();
            }
            else if (_currentSortType == HeroSortType.Rarity)
            {
                sortedHeroes = sortedHeroes.OrderBy(h => GetRarityPriority(h.Rarity)).ThenByDescending(h => h.Power).ToList();
            }

            var deployedNames = new HashSet<string>();
            int deployCount = 0;
            int heroIndex = 0;
            
            foreach (var activeSlot in activeSlots)
            {
                if (deployCount >= 9) break;

                // Tìm tướng tiếp theo chưa bị trùng tên
                while (heroIndex < sortedHeroes.Count)
                {
                    var hero = sortedHeroes[heroIndex];
                    heroIndex++;
                    
                    if (!deployedNames.Contains(hero.Name))
                    {
                        _tempFormation[activeSlot] = hero.Id;
                        deployedNames.Add(hero.Name);
                        deployCount++;
                        break;
                    }
                }
            }

            ToastManager.Instance.ShowBigToast(LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_auto_deploy_success"));
            RefreshFormationUI();
            RefreshOwnedHeroes();
        }

        private void OnStartCombatClicked()
        {
            Debug.Log($"[BattlePrepPanel] OnStartCombatClicked triggered. _tempFormation.Count = {_tempFormation.Count}");
            if (_tempFormation.Count == 0)
            {
                Debug.LogWarning("[BattlePrepPanel] Start combat aborted: _tempFormation is empty! User must place heroes on grid slots.");
                ToastManager.Instance.ShowBigToast(LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_empty_formation"));
                return;
            }

            try
            {
                Debug.Log("[BattlePrepPanel] Saving formation to server in background...");
                // Tự động lưu đội hình lên máy chủ (chạy nền song song, không await để tránh mạng lag gây đơ giao diện)
                var slotsToSend = new List<(int, long)>();
                foreach (var kv in _tempFormation)
                {
                    Debug.Log($"[BattlePrepPanel] Formed hero: slot {kv.Key} -> heroId {kv.Value}");
                    slotsToSend.Add((kv.Key, kv.Value));
                }
                _ = SaveFormationInBackground(slotsToSend);

                // Đồng bộ đội hình hiện tại lên static data
                Debug.Log($"[BattlePrepPanel] Syncing static data. Blessed slot: {_blessedSlotIndex}. StageName: {_stageData?.stageName}");
                CombatStartData.CurrentStage = _stageData;
                CombatStartData.Formation = new Dictionary<int, long>(_tempFormation);
                CombatStartData.BlessedSlotIndex = _blessedSlotIndex;

                Debug.Log("[BattlePrepPanel] Invoking StartCombatDirectlyInScene...");
                _ = StartCombatDirectlyInScene();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BattlePrepPanel] Lỗi nghiêm trọng khi khởi chạy Combat trực tiếp: {ex}");
            }
        }

        private async System.Threading.Tasks.Task SaveFormationInBackground(List<(int, long)> slotsToSend)
        {
            try
            {
                await DiscipleApi.SetFormationAsync(slotsToSend);
                Debug.Log("[BattlePrepPanel] Đã tự động lưu đội hình lên server thành công.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BattlePrepPanel] Tự động lưu đội hình lên server thất bại: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task StartCombatDirectlyInScene()
        {
            Debug.Log("[BattlePrepPanel] StartCombatDirectlyInScene begins.");
            try
            {
                List<CombatEntity> players = new List<CombatEntity>();
                List<CombatEntity> enemies = new List<CombatEntity>();

                // Lấy camera chính để đổi toạ độ từ màn hình (UI) ra thế giới (World Space)
                Camera mainCam = Camera.main;
                if (mainCam == null)
                {
                    mainCam = GameObject.FindFirstObjectByType<Camera>();
                }
                Debug.Log($"[BattlePrepPanel] mainCam: {(mainCam != null ? mainCam.name : "NULL")}");
                float zOffset = 10f; // Khoảng cách từ Camera tới vị trí spawn của tướng

                // 1. Spawn Phe Ta dựa vào vị trí của 9 ô UI hoặc tọa độ mặc định
                Debug.Log($"[BattlePrepPanel] Spawning player entities... Count: {_tempFormation.Count}");
                foreach (var kv in _tempFormation)
                {
                    int slotIndex = kv.Key;
                    long heroId = kv.Value;

                    var heroInstance = GameManager.Instance.PlayerHeroes.Find(h => h.Id == heroId);
                    if (heroInstance == null)
                    {
                        Debug.LogError($"[BattlePrepPanel] Player hero ID {heroId} not found in GameManager.Instance.PlayerHeroes!");
                        continue;
                    }

                    var config = HeroDataManager.Instance.GetHeroConfigByCodeOrName(heroInstance.Name);
                    if (config == null) config = HeroDataManager.Instance.GetHeroConfig(heroInstance.Id);

                    // Lấy toạ độ thế giới
                    Vector3 spawnPos = new Vector3(-3f + (slotIndex % 3) * 1.5f, 1f - (slotIndex / 3) * 1.5f, 0f);
                    if (_slotItems != null && slotIndex < _slotItems.Length && _slotItems[slotIndex] != null)
                    {
                        if (mainCam != null)
                        {
                            Vector3 uiPos = _slotItems[slotIndex].transform.position;
                            uiPos.z = zOffset;
                            spawnPos = mainCam.ScreenToWorldPoint(uiPos);
                            spawnPos.z = 0f;
                        }
                        else
                        {
                            spawnPos = _slotItems[slotIndex].transform.position;
                            spawnPos.z = 0f;
                        }
                    }
                    Debug.Log($"[BattlePrepPanel] Hero '{heroInstance.Name}' target spawnPos: {spawnPos} (slot index: {slotIndex})");

                    GameObject go = null;
                    string prefabAddr = (config != null) ? config.prefabAddress : "";
                    Debug.Log($"[BattlePrepPanel] Hero Addressable prefab address: '{prefabAddr}'");
                    if (!string.IsNullOrEmpty(prefabAddr) && !prefabAddr.Contains(" "))
                    {
                        try
                        {
                            Debug.Log($"[BattlePrepPanel] Calling Addressables.InstantiateAsync for '{prefabAddr}'...");
                            var handle = UnityEngine.AddressableAssets.Addressables.InstantiateAsync(prefabAddr, spawnPos, Quaternion.identity);
                            go = await handle.Task;
                            Debug.Log($"[BattlePrepPanel] Addressables.InstantiateAsync finished. Result GameObject is {(go != null ? "NOT NULL" : "NULL")}");
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[BattlePrepPanel] Addressables.InstantiateAsync threw exception for '{prefabAddr}': {ex}");
                            go = null;
                        }
                    }

                    if (go == null)
                    {
                        Debug.LogWarning($"[BattlePrepPanel] Addressables instantiating failed for '{prefabAddr}'. Creating fallback dummy GameObject.");
                        go = new GameObject($"Hero_{heroInstance.Name}");
                        go.transform.position = spawnPos;
                    }

                    CombatEntity entity = go.GetComponent<CombatEntity>();
                    if (entity == null) entity = go.AddComponent<CombatEntity>();

                    entity.isPlayer = true;
                    entity.entityName = heroInstance.Name;
                    entity.maxHP = 1000 + (heroInstance.Level * 100);
                    entity.currentHP = entity.maxHP;
                    entity.attack = 100 + (heroInstance.Level * 10);
                    entity.defense = 50 + (heroInstance.Level * 5);
                    entity.speed = 10 + (heroInstance.Level * 2);

                    // Áp dụng buff Blessed Slot
                    if (slotIndex == _blessedSlotIndex)
                    {
                        Debug.Log($"[BattlePrepPanel] Blessed Slot Buff applied to {entity.entityName}");
                        entity.attack = (int)(entity.attack * 1.25f);
                        entity.defense = (int)(entity.defense * 1.25f);
                    }

                    players.Add(entity);
                    Debug.Log($"[BattlePrepPanel] Spawning player entity {entity.entityName} completed. Stats: HP={entity.maxHP}, ATK={entity.attack}");
                }

                // 2. Spawn Phe Địch đối xứng sang bên phải màn hình
                // Lấy lưới toạ độ quái tương ứng
                int[] enemySlots = { 4 };
                if (_stageData.enemiesConfig.Count == 1) enemySlots = new int[] { 4 };
                else if (_stageData.enemiesConfig.Count == 2) enemySlots = new int[] { 3, 5 };
                else if (_stageData.enemiesConfig.Count == 3) enemySlots = new int[] { 3, 4, 5 };
                else if (_stageData.enemiesConfig.Count == 4) enemySlots = new int[] { 0, 2, 6, 8 };
                else enemySlots = new int[] { 0, 2, 4, 6, 8 };

                Debug.Log($"[BattlePrepPanel] Spawning enemy entities... Count: {_stageData.enemiesConfig.Count}");
                for (int i = 0; i < _stageData.enemiesConfig.Count; i++)
                {
                    if (i >= enemySlots.Length) break;
                    int targetSlot = enemySlots[i];

                    var config = _stageData.enemiesConfig[i];

                    // Lấy toạ độ thế giới mặc định
                    Vector3 spawnPos = new Vector3(3f + (targetSlot % 3) * 1.5f, 1f - (targetSlot / 3) * 1.5f, 0f);
                    if (_slotItems != null && targetSlot < _slotItems.Length && _slotItems[targetSlot] != null)
                    {
                        if (mainCam != null)
                        {
                            Vector3 uiPos = _slotItems[targetSlot].transform.position;
                            uiPos.x += Screen.width * 0.4f; // Dịch sang phải 40% chiều rộng màn hình
                            uiPos.z = zOffset;
                            spawnPos = mainCam.ScreenToWorldPoint(uiPos);
                            spawnPos.z = 0f;
                        }
                        else
                        {
                            spawnPos = _slotItems[targetSlot].transform.position;
                            spawnPos.x += 5f;
                            spawnPos.z = 0f;
                        }
                    }
                    else
                    {
                        if (mainCam != null)
                        {
                            Vector3 fallbackUiPos = new Vector3(Screen.width * 0.7f, Screen.height * 0.5f, zOffset);
                            spawnPos = mainCam.ScreenToWorldPoint(fallbackUiPos);
                            spawnPos.z = 0f;
                        }
                    }
                    Debug.Log($"[BattlePrepPanel] Enemy '{config.name}' target spawnPos: {spawnPos} (slot index: {targetSlot})");

                    GameObject go = null;
                    string enemyPrefabAddr = config.prefabAddress;
                    Debug.Log($"[BattlePrepPanel] Enemy Addressable prefab address: '{enemyPrefabAddr}'");
                    if (!string.IsNullOrEmpty(enemyPrefabAddr) && !enemyPrefabAddr.Contains(" "))
                    {
                        try
                        {
                            Debug.Log($"[BattlePrepPanel] Calling Addressables.InstantiateAsync for enemy '{enemyPrefabAddr}'...");
                            var handle = UnityEngine.AddressableAssets.Addressables.InstantiateAsync(enemyPrefabAddr, spawnPos, Quaternion.Euler(0f, 180f, 0f));
                            go = await handle.Task;
                            Debug.Log($"[BattlePrepPanel] Addressables.InstantiateAsync finished for enemy. Result GameObject is {(go != null ? "NOT NULL" : "NULL")}");
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[BattlePrepPanel] Addressables.InstantiateAsync threw exception for enemy '{enemyPrefabAddr}': {ex}");
                            go = null;
                        }
                    }

                    if (go == null && config.prefabVisual != null)
                    {
                        Debug.Log("[BattlePrepPanel] Falling back to config.prefabVisual instantiation for enemy.");
                        go = Instantiate(config.prefabVisual, spawnPos, Quaternion.Euler(0f, 180f, 0f));
                    }

                    if (go == null)
                    {
                        Debug.LogWarning($"[BattlePrepPanel] Enemy instantiating failed for '{config.name}'. Creating fallback dummy GameObject.");
                        go = new GameObject($"Enemy_{config.name}");
                        go.transform.position = spawnPos;
                        go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    }

                    CombatEntity entity = go.GetComponent<CombatEntity>();
                    if (entity == null) entity = go.AddComponent<CombatEntity>();

                    entity.isPlayer = false;
                    string localizedMonsterName = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, config.name);
                    entity.entityName = !string.IsNullOrEmpty(localizedMonsterName) ? localizedMonsterName : config.name;
                    entity.maxHP = config.maxHP;
                    entity.currentHP = config.maxHP;
                    entity.attack = config.attack;
                    entity.defense = config.defense;
                    entity.speed = config.speed;

                    enemies.Add(entity);
                    Debug.Log($"[BattlePrepPanel] Spawning enemy entity {entity.entityName} completed. Stats: HP={entity.maxHP}, ATK={entity.attack}");
                }

                // 3. Khởi chạy Combat Logic
                if (CombatManager.Instance == null)
                {
                    Debug.Log("[BattlePrepPanel] CombatManager.Instance is null. Creating new CombatManager GameObject in scene.");
                    var managerObj = new GameObject("CombatManager");
                    managerObj.AddComponent<CombatManager>();
                }
                Debug.Log($"[BattlePrepPanel] Starting combat via CombatManager.Instance. Players count: {players.Count}, Enemies count: {enemies.Count}");
                CombatManager.Instance.StartCombat(players, enemies);

                // 4. Quản lý UI: Đóng bảng Prep, Mở bảng HUD Combat
                Debug.Log("[BattlePrepPanel] Closing Prep panel and opening CombatHUD panel.");
                Hide();
                UIManager.Instance.OpenPanel("CombatHUD");
                Debug.Log("[BattlePrepPanel] Transition to CombatHUD complete.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BattlePrepPanel] StartCombatDirectlyInScene failed with exception: {ex}");
            }
        }

        private void OnRuleClicked()
        {
            string ruleTitle = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_rule_title");
            if (string.IsNullOrEmpty(ruleTitle) || ruleTitle.StartsWith("[")) ruleTitle = "Quy Tắc Trận Hình";

            string ruleContent = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_prep_rule_content");
            if (string.IsNullOrEmpty(ruleContent) || ruleContent.StartsWith("["))
            {
                ruleContent = "1. Trận hình chuẩn bị đấu gồm lưới 3x3 phe ta (tổng cộng 9 ô).\n" +
                              "2. Có thể chọn thay đổi các hình trận hình mẫu khác nhau để tự do sáng tạo vị trí đứng.\n" +
                              "3. Khi thay đổi sơ đồ trận hình, các ô không thuộc sơ đồ mới sẽ tự động bị Khóa.\n" +
                              "4. Mỗi trận đấu sẽ kích hoạt ngẫu nhiên 1 ô Cát Tường phe ta. Tướng đứng ở ô Cát Tường sẽ nhận thêm buff đặc biệt: tăng 25% Công & Thủ.\n" +
                              "5. Sử dụng nút 'Xếp đội nhanh' để tự động nạp lại đội hình đã lưu của bạn.\n" +
                              "6. Sử dụng nút 'Hạ trận nhanh' để tự động lấy các tướng mạnh nhất trong danh sách xếp vào trận.";
            }

            UIManager.Instance.ShowMessage(ruleTitle, ruleContent);
        }
    }

    public static class CombatStartData
    {
        public static StageData CurrentStage;
        public static Dictionary<int, long> Formation;
        public static int BlessedSlotIndex = -1;
    }
}
