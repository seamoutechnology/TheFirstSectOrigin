using UnityEngine;
using System.Collections.Generic;
using GameClient.Gameplay.World;
using GameClient.Managers;
using GameClient.UI;
using UnityEngine.AddressableAssets;
using GameClient.Battle;

namespace GameClient.Gameplay.Combat
{
    public class CombatSceneController : MonoBehaviour
    {
        [Header("Spawn Containers")]
        [SerializeField] private Transform playerSpawnContainer;
        [SerializeField] private Transform enemySpawnContainer;

        private Transform[] playerSpawnPoints;
        private Transform[] enemySpawnPoints;

        private void Awake()
        {
            // 1. Setup Player Spawn Points
            if (playerSpawnContainer == null)
            {
                var playerRoot = transform.Find("PlayerSpawnPoints");
                if (playerRoot != null)
                {
                    playerSpawnContainer = playerRoot;
                }
                else
                {
                    var newGo = new GameObject("PlayerSpawnPoints");
                    newGo.transform.SetParent(transform);
                    newGo.transform.position = transform.position;
                    newGo.transform.rotation = Quaternion.identity;
                    newGo.transform.localScale = Vector3.one;
                    playerSpawnContainer = newGo.transform;
                }
            }

            bool isPlayerUI = playerSpawnContainer.GetComponentInParent<Canvas>() != null;
            if (playerSpawnContainer.childCount > 0)
            {
                var points = new List<Transform>();
                foreach (Transform child in playerSpawnContainer)
                {
                    if (isPlayerUI && child.GetComponent<RectTransform>() == null)
                    {
                        child.gameObject.AddComponent<RectTransform>();
                    }
                    if (child.name == "P_Row0_Col0")
                    {
                        child.localScale = new Vector3(3.6f, 3.6f, 3.6f);
                    }
                    else
                    {
                        child.localScale = new Vector3(3f, 3f, 3f);
                    }
                    points.Add(child);
                }
                playerSpawnPoints = points.ToArray();
                Debug.Log($"[CombatSceneController] Found {playerSpawnPoints.Length} player spawn points in playerSpawnContainer.");
            }
            else
            {
                var points = new List<Transform>();
                // Tạo lưới 3x3 cho phe ta (9 ô)
                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        Transform p;
                        if (isPlayerUI)
                        {
                            p = new GameObject($"P_Row{r}_Col{c}", typeof(RectTransform)).transform;
                        }
                        else
                        {
                            p = new GameObject($"P_Row{r}_Col{c}").transform;
                        }
                        p.SetParent(playerSpawnContainer);
                        p.localPosition = new Vector3(-4.5f + c * 1.2f, 1.2f - r * 1.2f, 0f);
                        p.localRotation = Quaternion.identity;
                        if (r == 0 && c == 0)
                        {
                            p.localScale = new Vector3(3.6f, 3.6f, 3.6f);
                        }
                        else
                        {
                            p.localScale = new Vector3(3f, 3f, 3f);
                        }
                        points.Add(p);
                    }
                }
                playerSpawnPoints = points.ToArray();
                Debug.Log("[CombatSceneController] Created programmatic 3x3 player spawn points in playerSpawnContainer.");
            }

            // 2. Setup Enemy Spawn Points
            if (enemySpawnContainer == null)
            {
                var enemyRoot = transform.Find("EnemySpawnPoints");
                if (enemyRoot != null)
                {
                    enemySpawnContainer = enemyRoot;
                }
                else
                {
                    var newGo = new GameObject("EnemySpawnPoints");
                    newGo.transform.SetParent(transform);
                    newGo.transform.position = transform.position;
                    newGo.transform.rotation = Quaternion.identity;
                    newGo.transform.localScale = Vector3.one;
                    enemySpawnContainer = newGo.transform;
                }
            }

            bool isEnemyUI = enemySpawnContainer.GetComponentInParent<Canvas>() != null;
            if (enemySpawnContainer.childCount > 0)
            {
                var points = new List<Transform>();
                foreach (Transform child in enemySpawnContainer)
                {
                    if (isEnemyUI && child.GetComponent<RectTransform>() == null)
                    {
                        child.gameObject.AddComponent<RectTransform>();
                    }
                    child.localRotation = Quaternion.identity; // Giữ nguyên hướng mặt tự nhiên (hướng sang trái)
                    child.localScale = new Vector3(3f, 3f, 3f);
                    points.Add(child);
                }
                enemySpawnPoints = points.ToArray();
                Debug.Log($"[CombatSceneController] Found {enemySpawnPoints.Length} enemy spawn points in enemySpawnContainer.");
            }
            else
            {
                var points = new List<Transform>();
                // Tạo lưới 3x3 cho phe địch (9 ô)
                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        Transform e;
                        if (isEnemyUI)
                        {
                            e = new GameObject($"E_Row{r}_Col{c}", typeof(RectTransform)).transform;
                        }
                        else
                        {
                            e = new GameObject($"E_Row{r}_Col{c}").transform;
                        }
                        e.SetParent(enemySpawnContainer);
                        e.localPosition = new Vector3(2.1f + c * 1.2f, 1.2f - r * 1.2f, 0f);
                        e.localRotation = Quaternion.identity; // Giữ nguyên hướng mặt tự nhiên (hướng sang trái)
                        e.localScale = new Vector3(3f, 3f, 3f);
                        points.Add(e);
                    }
                }
                enemySpawnPoints = points.ToArray();
                Debug.Log("[CombatSceneController] Created programmatic 3x3 enemy spawn points in enemySpawnContainer.");
            }
        }

        private void Start()
        {
            if (CombatStartData.CurrentStage == null)
            {
                Debug.LogWarning("[CombatSceneController] StageData is null. Loading fallback Stage config.");
                LoadFallbackStage();
                return;
            }

            InitializeCombat();
        }

        private void LoadFallbackStage()
        {
            var fallbackStage = Resources.Load<StageData>("GameData/Stages/Stage_Fallback");
            if (fallbackStage != null)
            {
                CombatStartData.CurrentStage = fallbackStage;
                CombatStartData.Formation = new Dictionary<int, long>();
                
                var owned = GameManager.Instance.PlayerHeroes;
                if (owned != null)
                {
                    // Đặt ngẫu nhiên 3 tướng vào các ô trung tâm (1, 4, 7) để test
                    int[] slots = { 1, 4, 7 };
                    for (int i = 0; i < Mathf.Min(3, owned.Count); i++)
                    {
                        CombatStartData.Formation[slots[i]] = owned[i].Id;
                    }
                }
                
                InitializeCombat();
            }
            else
            {
                Debug.LogError("[CombatSceneController] Cannot find Stage_Fallback in Resources!");
            }
        }

        private async void InitializeCombat()
        {
            // Cấu hình Camera chính hội tụ vào vị trí đấu (CombatController)
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.orthographic = true;
                mainCam.orthographicSize = 5f;
                mainCam.transform.position = new Vector3(transform.position.x, transform.position.y, -10f);
                mainCam.transform.rotation = Quaternion.identity;
                Debug.Log($"[CombatSceneController] Centered Camera at {mainCam.transform.position}");
            }

            var stage = CombatStartData.CurrentStage;
            List<CombatEntity> players = new List<CombatEntity>();
            List<CombatEntity> enemies = new List<CombatEntity>();

            // Sử dụng ô buff cát tường được chọn trước từ màn hình chuẩn bị (UI)
            int buffSlotIndex = CombatStartData.BlessedSlotIndex;
            string announcement = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_log_blessed_announcement", buffSlotIndex + 1);
            Debug.Log(announcement);

            // 1. Spawning Player Team (Tướng phe ta theo đúng vị trí lưới 9 ô)
            Debug.Log($"[CombatSceneController] Starting Player team spawn. Formation count: {(CombatStartData.Formation != null ? CombatStartData.Formation.Count.ToString() : "NULL")}");
            if (CombatStartData.Formation != null)
            {
                foreach (var kv in CombatStartData.Formation)
                {
                    int slotIndex = kv.Key;
                    long heroId = kv.Value;

                    if (slotIndex < 0 || slotIndex >= playerSpawnPoints.Length)
                    {
                        Debug.LogError($"[CombatSceneController] Slot index {slotIndex} out of spawn points bounds (Max: {playerSpawnPoints.Length})");
                        continue;
                    }

                    var heroInstance = GameManager.Instance.PlayerHeroes.Find(h => h.Id == heroId);
                    if (heroInstance == null)
                    {
                        Debug.LogError($"[CombatSceneController] Cannot find player hero with ID {heroId} in GameManager.");
                        continue;
                    }

                    var config = HeroDataManager.Instance.GetHeroConfigByName(heroInstance.Name);
                    if (config == null) config = HeroDataManager.Instance.GetHeroConfig(heroInstance.Id);

                    GameObject go = null;
                    string prefabAddr = (config != null) ? config.prefabAddress : "";
                    Debug.Log($"[CombatSceneController] Spawning player '{heroInstance.Name}' at slot {slotIndex}. Prefab Address: '{prefabAddr}'");

                    var slotItem = playerSpawnPoints[slotIndex].GetComponent<UI_FormationSlotItem>();
                    if (slotItem != null)
                    {
                        go = playerSpawnPoints[slotIndex].gameObject;
                        if (!string.IsNullOrEmpty(prefabAddr) && !prefabAddr.Contains(" "))
                        {
                            try
                            {
                                var sprite = await Addressables.LoadAssetAsync<Sprite>(prefabAddr).Task;
                                slotItem.SetHeroVisual(sprite, sprite != null);
                                slotItem.SetFlipped(true); // Quay mặt sang phải (hướng đối thủ)
                                slotItem.SetTextBgActive(true);
                                slotItem.SetStatusText(heroInstance.Name);
                                Debug.Log($"[CombatSceneController] Successfully set player '{heroInstance.Name}' visual on UI Slot.");
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogWarning($"[CombatSceneController] Failed loading Addressable for '{heroInstance.Name}' on UI Slot: {ex.Message}.");
                                slotItem.SetHeroVisual(null, false);
                                slotItem.SetTextBgActive(true);
                                slotItem.SetStatusText(heroInstance.Name);
                            }
                        }
                        else
                        {
                            slotItem.SetHeroVisual(null, false);
                            slotItem.SetTextBgActive(true);
                            slotItem.SetStatusText(heroInstance.Name);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(prefabAddr) && !prefabAddr.Contains(" "))
                        {
                            try
                            {
                                if (prefabAddr.EndsWith("_img") || prefabAddr.Contains("char_"))
                                {
                                    bool isUI = playerSpawnPoints[slotIndex].GetComponentInParent<Canvas>() != null;
                                    if (isUI)
                                    {
                                        go = new GameObject($"Hero_{heroInstance.Name}", typeof(RectTransform));
                                        go.transform.SetParent(playerSpawnPoints[slotIndex], false);
                                        var rect = go.GetComponent<RectTransform>();
                                        rect.anchoredPosition = Vector2.zero;
                                        rect.localRotation = Quaternion.identity;
                                        go.transform.localScale = new Vector3(-1f, 1f, 1f); // Flip horizontally so it looks right
                                        var parentRect = playerSpawnPoints[slotIndex].GetComponent<RectTransform>();
                                        rect.sizeDelta = parentRect != null ? parentRect.sizeDelta : new Vector2(120f, 120f);
                                        
                                        var img = go.AddComponent<UnityEngine.UI.Image>();
                                        img.preserveAspect = true;
                                        var sprite = await Addressables.LoadAssetAsync<Sprite>(prefabAddr).Task;
                                        img.sprite = sprite;
                                    }
                                    else
                                    {
                                        go = new GameObject($"Hero_{heroInstance.Name}");
                                        go.transform.SetParent(playerSpawnPoints[slotIndex]);
                                        go.transform.localPosition = Vector3.zero;
                                        go.transform.localRotation = Quaternion.identity;
                                        var pScale = playerSpawnPoints[slotIndex].localScale;
                                        float sx = 10f / (pScale.x != 0 ? pScale.x : 1f);
                                        float sy = 10f / (pScale.y != 0 ? pScale.y : 1f);
                                        go.transform.localScale = new Vector3(sx, sy, 1f);
                                        
                                        var sr = go.AddComponent<SpriteRenderer>();
                                        var sprite = await Addressables.LoadAssetAsync<Sprite>(prefabAddr).Task;
                                        sr.sprite = sprite;
                                        sr.sortingOrder = 10; // Đảm bảo hiển thị trên nền
                                    }
                                }
                                else
                                {
                                    go = await Addressables.InstantiateAsync(config.prefabAddress, playerSpawnPoints[slotIndex].position, playerSpawnPoints[slotIndex].rotation).Task;
                                    go.transform.SetParent(playerSpawnPoints[slotIndex]);
                                }
                                Debug.Log($"[CombatSceneController] Successfully loaded player '{heroInstance.Name}' visual.");
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogWarning($"[CombatSceneController] Failed loading Addressable for '{heroInstance.Name}': {ex.Message}. Using fallback.");
                                if (go != null) Destroy(go);
                                go = null;
                            }
                        }

                        if (go == null)
                        {
                            // Dùng hình trụ Capsule 3D để hiển thị nếu lỗi tải mô hình nhân vật
                            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                            go.name = $"Hero_{heroInstance.Name}_Fallback";
                            go.transform.SetParent(playerSpawnPoints[slotIndex]);
                            go.transform.localPosition = Vector3.zero;
                            go.transform.localRotation = Quaternion.identity;
                            var pScale = playerSpawnPoints[slotIndex].localScale;
                            float sx = 10f / (pScale.x != 0 ? pScale.x : 1f);
                            float sy = 10f / (pScale.y != 0 ? pScale.y : 1f);
                            go.transform.localScale = new Vector3(sx, sy, 1f);
                            
                            var sr = go.AddComponent<SpriteRenderer>();
                            sr.sortingOrder = 10;
                            try
                            {
                                var sprite = await Addressables.LoadAssetAsync<Sprite>(prefabAddr).Task;
                                sr.sprite = sprite;
                            }
                            catch {}
                            Debug.LogWarning($"[CombatSceneController] Created fallback for '{heroInstance.Name}' at {go.transform.position}");
                        }
                    }

                    CombatEntity entity = go.GetComponent<CombatEntity>();
                    if (entity == null) entity = go.AddComponent<CombatEntity>();

                    entity.isPlayer = true;
                    entity.entityName = heroInstance.Name;
                    if (heroInstance.Skills != null)
                    {
                        entity.skills.AddRange(heroInstance.Skills);
                    }
                    
                    // Chỉ số gốc dựa vào cấp độ
                    entity.maxHP = 1000 + (heroInstance.Level * 100);
                    entity.currentHP = entity.maxHP;
                    entity.attack = 100 + (heroInstance.Level * 10);
                    entity.defense = 50 + (heroInstance.Level * 5);
                    entity.speed = 10 + (heroInstance.Level * 2);

                    // Áp dụng buff 25% công & thủ nếu đứng ở ô buff ngẫu nhiên
                    if (slotIndex == buffSlotIndex)
                    {
                        entity.attack = (int)(entity.attack * 1.25f);
                        entity.defense = (int)(entity.defense * 1.25f);
                        string appliedLog = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_log_blessed_applied", entity.entityName, slotIndex + 1);
                        Debug.Log(appliedLog);
                    }

                    var visual = go.GetComponent<HeroVisual>();
                    if (visual == null) visual = go.AddComponent<HeroVisual>();

                    players.Add(entity);
                }
            }

            // Quy định vị trí xuất hiện của phe địch dựa trên số lượng (không dùng buff trận hình)
            int[] enemySlots;
            if (stage.enemiesConfig.Count == 1) enemySlots = new int[] { 4 }; // Giữa
            else if (stage.enemiesConfig.Count == 2) enemySlots = new int[] { 3, 5 }; // Hai bên hàng giữa
            else if (stage.enemiesConfig.Count == 3) enemySlots = new int[] { 3, 4, 5 }; // Hàng giữa dọc
            else if (stage.enemiesConfig.Count == 4) enemySlots = new int[] { 0, 2, 6, 8 }; // 4 góc
            else enemySlots = new int[] { 0, 2, 4, 6, 8 }; // Hình chữ X

            // 2. Spawning Enemies (Phe địch tối đa 5 con xếp trên lưới 3x3)
            Debug.Log($"[CombatSceneController] Starting Enemy spawn. Configured count: {stage.enemiesConfig.Count}");
            for (int i = 0; i < stage.enemiesConfig.Count; i++)
            {
                if (i >= enemySlots.Length) break;
                int targetSlot = enemySlots[i];
                if (targetSlot >= enemySpawnPoints.Length)
                {
                    Debug.LogError($"[CombatSceneController] Enemy slot {targetSlot} out of spawn points bounds (Max: {enemySpawnPoints.Length})");
                    break;
                }

                var config = stage.enemiesConfig[i];
                GameObject go = null;
                Vector3 spawnPos = enemySpawnPoints[targetSlot].position;
                Quaternion spawnRot = enemySpawnPoints[targetSlot].rotation;
                Debug.Log($"[CombatSceneController] Spawning enemy '{config.name}' at slot {targetSlot}. Address: '{config.prefabAddress}'");

                var slotItem = enemySpawnPoints[targetSlot].GetComponent<UI_FormationSlotItem>();
                if (slotItem != null)
                {
                    go = enemySpawnPoints[targetSlot].gameObject;
                    if (!string.IsNullOrEmpty(config.prefabAddress) && !config.prefabAddress.Contains(" ") && config.prefabAddress != "MonsterPrefab")
                    {
                        try
                        {
                            var sprite = await Addressables.LoadAssetAsync<Sprite>(config.prefabAddress).Task;
                            slotItem.SetHeroVisual(sprite, sprite != null);
                            slotItem.SetFlipped(false); // Quay mặt sang trái (hướng ta)
                            slotItem.SetTextBgActive(true);
                            slotItem.SetStatusText(config.name);
                            Debug.Log($"[CombatSceneController] Successfully set enemy '{config.name}' visual on UI Slot.");
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[CombatSceneController] Failed loading Addressable for enemy '{config.name}' on UI Slot: {ex.Message}.");
                            slotItem.SetHeroVisual(null, false);
                            slotItem.SetTextBgActive(true);
                            slotItem.SetStatusText(config.name);
                        }
                    }
                    else
                    {
                        slotItem.SetHeroVisual(null, false);
                        slotItem.SetTextBgActive(true);
                        slotItem.SetStatusText(config.name);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(config.prefabAddress) && !config.prefabAddress.Contains(" ") && config.prefabAddress != "MonsterPrefab")
                    {
                        try
                        {
                            if (config.prefabAddress.EndsWith("_img") || config.prefabAddress.Contains("char_") || config.prefabAddress.Contains("enemy_"))
                            {
                                bool isUI = enemySpawnPoints[targetSlot].GetComponentInParent<Canvas>() != null;
                                if (isUI)
                                {
                                    go = new GameObject($"Enemy_{config.name}", typeof(RectTransform));
                                    go.transform.SetParent(enemySpawnPoints[targetSlot], false);
                                    var rect = go.GetComponent<RectTransform>();
                                    rect.anchoredPosition = Vector2.zero;
                                    rect.localRotation = Quaternion.identity;
                                    var parentRect = enemySpawnPoints[targetSlot].GetComponent<RectTransform>();
                                    rect.sizeDelta = parentRect != null ? parentRect.sizeDelta : new Vector2(120f, 120f);
                                    
                                    var img = go.AddComponent<UnityEngine.UI.Image>();
                                    img.preserveAspect = true;
                                    var sprite = await Addressables.LoadAssetAsync<Sprite>(config.prefabAddress).Task;
                                    img.sprite = sprite;
                                }
                                else
                                {
                                    go = new GameObject($"Enemy_{config.name}");
                                    go.transform.SetParent(enemySpawnPoints[targetSlot]);
                                    go.transform.localPosition = Vector3.zero;
                                    go.transform.localRotation = Quaternion.identity; // Trực thuộc transform con đã xoay 180
                                    var pScale = enemySpawnPoints[targetSlot].localScale;
                                    float sx = 10f / (pScale.x != 0 ? pScale.x : 1f);
                                    float sy = 10f / (pScale.y != 0 ? pScale.y : 1f);
                                    go.transform.localScale = new Vector3(sx, sy, 1f);
                                    
                                    var sr = go.AddComponent<SpriteRenderer>();
                                    var sprite = await Addressables.LoadAssetAsync<Sprite>(config.prefabAddress).Task;
                                    sr.sprite = sprite;
                                    sr.sortingOrder = 10; // Đảm bảo hiển thị trên nền
                                }
                            }
                            else
                            {
                                go = await Addressables.InstantiateAsync(config.prefabAddress, spawnPos, spawnRot).Task;
                                go.transform.SetParent(enemySpawnPoints[targetSlot]);
                            }
                            Debug.Log($"[CombatSceneController] Successfully loaded enemy '{config.name}' visual.");
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[CombatSceneController] Failed loading Addressable for enemy '{config.name}': {ex.Message}. Using fallback.");
                            if (go != null) Destroy(go);
                            go = null;
                        }
                    }

                    if (go == null && config.prefabVisual != null)
                    {
                        Debug.Log($"[CombatSceneController] Instantiating config.prefabVisual for enemy '{config.name}'.");
                        go = Instantiate(config.prefabVisual, spawnPos, spawnRot);
                        go.transform.SetParent(enemySpawnPoints[targetSlot]);
                    }

                    if (go == null)
                    {
                        // Tạo GameObject rỗng làm cha để chứa SpriteRenderer, tránh xung đột với MeshRenderer của Cube
                        go = new GameObject($"Enemy_{config.name}_Fallback");
                        go.transform.SetParent(enemySpawnPoints[targetSlot]);
                        go.transform.localPosition = Vector3.zero;
                        go.transform.localRotation = Quaternion.identity;
                        var pScale = enemySpawnPoints[targetSlot].localScale;
                        float sx = 10f / (pScale.x != 0 ? pScale.x : 1f);
                        float sy = 10f / (pScale.y != 0 ? pScale.y : 1f);
                        go.transform.localScale = new Vector3(sx, sy, 1f);
                        
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sortingOrder = 10;
                        try
                        {
                            if (!string.IsNullOrEmpty(config.prefabAddress) && config.prefabAddress != "MonsterPrefab")
                            {
                                var sprite = await Addressables.LoadAssetAsync<Sprite>(config.prefabAddress).Task;
                                sr.sprite = sprite;
                            }
                        }
                        catch {}

                        // Nếu không tải được sprite nào, tạo một Cube 3D con làm hiển thị hình khối tạm thời
                        if (sr.sprite == null)
                        {
                            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            cube.transform.SetParent(go.transform, false);
                            cube.transform.localPosition = Vector3.zero;
                            cube.transform.localScale = Vector3.one;
                        }
                        Debug.LogWarning($"[CombatSceneController] Created fallback for enemy '{config.name}' at {go.transform.position}");
                    }
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
            }

            // 3. Khởi chạy Combat Manager
            if (CombatManager.Instance == null)
            {
                Debug.Log("[CombatSceneController] CombatManager.Instance is null. Creating new CombatManager GameObject in scene.");
                var managerObj = new GameObject("CombatManager");
                managerObj.AddComponent<CombatManager>();
            }
            CombatManager.Instance.EnemyID = stage != null ? stage.stageId : "Stage_Fallback";
            Debug.Log($"[CombatSceneController] All spawned. Starting combat via CombatManager. Stage: {CombatManager.Instance.EnemyID}, Players: {players.Count}, Enemies: {enemies.Count}");
            CombatManager.Instance.StartCombat(players, enemies);

            // 4. Mở HUD Combat
            Debug.Log("[CombatSceneController] Opening CombatHUD panel...");
            UIManager.Instance.OpenPanel("CombatHUD");
            Debug.Log("[CombatSceneController] InitializeCombat finished.");
        }
    }
}
