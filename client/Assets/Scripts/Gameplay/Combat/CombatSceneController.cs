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
        [Header("Spawn Points")]
        [SerializeField] private Transform[] playerSpawnPoints;
        [SerializeField] private Transform[] enemySpawnPoints;

        private void Awake()
        {
            // Tự động tạo các điểm Spawn nếu không được thiết lập trong Inspector
            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
            {
                var playerRoot = new GameObject("PlayerSpawnPoints").transform;
                playerRoot.SetParent(transform);
                var points = new List<Transform>();
                
                // Tạo lưới 3x3 cho phe ta (9 ô)
                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        var p = new GameObject($"P_Row{r}_Col{c}").transform;
                        p.SetParent(playerRoot);
                        p.localPosition = new Vector3(-4.5f + c * 1.2f, 1.2f - r * 1.2f, 0f);
                        p.localRotation = Quaternion.identity;
                        points.Add(p);
                    }
                }
                playerSpawnPoints = points.ToArray();
            }

            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                var enemyRoot = new GameObject("EnemySpawnPoints").transform;
                enemyRoot.SetParent(transform);
                var points = new List<Transform>();
                
                // Tạo lưới 3x3 cho phe địch (9 ô)
                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        var e = new GameObject($"E_Row{r}_Col{c}").transform;
                        e.SetParent(enemyRoot);
                        e.localPosition = new Vector3(2.1f + c * 1.2f, 1.2f - r * 1.2f, 0f);
                        e.localRotation = Quaternion.Euler(0f, 180f, 0f); // Quay mặt sang trái
                        points.Add(e);
                    }
                }
                enemySpawnPoints = points.ToArray();
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
            var stage = CombatStartData.CurrentStage;
            List<CombatEntity> players = new List<CombatEntity>();
            List<CombatEntity> enemies = new List<CombatEntity>();

            // Sử dụng ô buff cát tường được chọn trước từ màn hình chuẩn bị (UI)
            int buffSlotIndex = CombatStartData.BlessedSlotIndex;
            string announcement = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_log_blessed_announcement", buffSlotIndex + 1);
            Debug.Log(announcement);

            // 1. Spawning Player Team (Tướng phe ta theo đúng vị trí lưới 9 ô)
            if (CombatStartData.Formation != null)
            {
                foreach (var kv in CombatStartData.Formation)
                {
                    int slotIndex = kv.Key;
                    long heroId = kv.Value;

                    if (slotIndex < 0 || slotIndex >= playerSpawnPoints.Length) continue;

                    var heroInstance = GameManager.Instance.PlayerHeroes.Find(h => h.Id == heroId);
                    if (heroInstance == null) continue;

                    var config = HeroDataManager.Instance.GetHeroConfigByName(heroInstance.Name);
                    if (config == null) config = HeroDataManager.Instance.GetHeroConfig(heroInstance.Id);

                    GameObject go;
                    if (config != null && !string.IsNullOrEmpty(config.prefabAddress))
                    {
                        try
                        {
                            go = await Addressables.InstantiateAsync(config.prefabAddress, playerSpawnPoints[slotIndex].position, playerSpawnPoints[slotIndex].rotation).Task;
                        }
                        catch (System.Exception)
                        {
                            go = new GameObject($"Hero_{heroInstance.Name}");
                            go.transform.position = playerSpawnPoints[slotIndex].position;
                            go.transform.rotation = playerSpawnPoints[slotIndex].rotation;
                        }
                    }
                    else
                    {
                        go = new GameObject($"Hero_{heroInstance.Name}");
                        go.transform.position = playerSpawnPoints[slotIndex].position;
                        go.transform.rotation = playerSpawnPoints[slotIndex].rotation;
                    }

                    CombatEntity entity = go.GetComponent<CombatEntity>();
                    if (entity == null) entity = go.AddComponent<CombatEntity>();

                    entity.isPlayer = true;
                    entity.entityName = heroInstance.Name;
                    
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
            for (int i = 0; i < stage.enemiesConfig.Count; i++)
            {
                if (i >= enemySlots.Length) break;
                int targetSlot = enemySlots[i];
                if (targetSlot >= enemySpawnPoints.Length) break;

                var config = stage.enemiesConfig[i];
                GameObject go = null;
                Vector3 spawnPos = enemySpawnPoints[targetSlot].position;
                Quaternion spawnRot = enemySpawnPoints[targetSlot].rotation;

                if (!string.IsNullOrEmpty(config.prefabAddress))
                {
                    try
                    {
                        go = await Addressables.InstantiateAsync(config.prefabAddress, spawnPos, spawnRot).Task;
                    }
                    catch (System.Exception)
                    {
                        go = null;
                    }
                }

                if (go == null && config.prefabVisual != null)
                {
                    go = Instantiate(config.prefabVisual, spawnPos, spawnRot);
                }

                if (go == null)
                {
                    go = new GameObject($"Enemy_{config.name}");
                    go.transform.position = spawnPos;
                    go.transform.rotation = spawnRot;
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
            CombatManager.Instance.StartCombat(players, enemies);

            // 4. Mở HUD Combat
            UIManager.Instance.OpenPanel("CombatHUD");
        }
    }
}
