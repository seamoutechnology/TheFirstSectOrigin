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
            // Tải cấu hình ải dự phòng để nhà phát triển test trực tiếp trong Scene
            var fallbackStage = Resources.Load<StageData>("GameData/Stages/Stage_Fallback");
            if (fallbackStage != null)
            {
                CombatStartData.CurrentStage = fallbackStage;
                // Lấy 3 tướng đầu tiên của người chơi để test
                CombatStartData.SelectedHeroIds = new List<long>();
                var owned = GameManager.Instance.PlayerHeroes;
                if (owned != null)
                {
                    for (int i = 0; i < Mathf.Min(3, owned.Count); i++)
                    {
                        CombatStartData.SelectedHeroIds.Add(owned[i].Id);
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

            // 1. Spawning Player Team (Tướng phe ta)
            if (CombatStartData.SelectedHeroIds != null)
            {
                for (int i = 0; i < CombatStartData.SelectedHeroIds.Count; i++)
                {
                    if (i >= playerSpawnPoints.Length) break;

                    long heroId = CombatStartData.SelectedHeroIds[i];
                    var heroInstance = GameManager.Instance.PlayerHeroes.Find(h => h.Id == heroId);
                    if (heroInstance == null) continue;

                    var config = HeroDataManager.Instance.GetHeroConfigByName(heroInstance.Name);
                    if (config == null) config = HeroDataManager.Instance.GetHeroConfig(heroInstance.Id);

                    GameObject go;
                    if (config != null && !string.IsNullOrEmpty(config.prefabAddress))
                    {
                        try
                        {
                            go = await Addressables.InstantiateAsync(config.prefabAddress, playerSpawnPoints[i].position, playerSpawnPoints[i].rotation).Task;
                        }
                        catch (System.Exception)
                        {
                            go = new GameObject($"Hero_{heroInstance.Name}");
                            go.transform.position = playerSpawnPoints[i].position;
                            go.transform.rotation = playerSpawnPoints[i].rotation;
                        }
                    }
                    else
                    {
                        go = new GameObject($"Hero_{heroInstance.Name}");
                        go.transform.position = playerSpawnPoints[i].position;
                        go.transform.rotation = playerSpawnPoints[i].rotation;
                    }

                    CombatEntity entity = go.GetComponent<CombatEntity>();
                    if (entity == null) entity = go.AddComponent<CombatEntity>();

                    entity.isPlayer = true;
                    entity.entityName = heroInstance.Name;
                    
                    // Stats based on character levels/stars
                    entity.maxHP = 1000 + (heroInstance.Level * 100);
                    entity.currentHP = entity.maxHP;
                    entity.attack = 100 + (heroInstance.Level * 10);
                    entity.defense = 50 + (heroInstance.Level * 5);
                    entity.speed = 10 + (heroInstance.Level * 2);

                    var visual = go.GetComponent<HeroVisual>();
                    if (visual == null) visual = go.AddComponent<HeroVisual>();

                    players.Add(entity);
                }
            }

            // 2. Spawning Enemies / Boss (Phe địch)
            for (int i = 0; i < stage.enemiesConfig.Count; i++)
            {
                if (i >= enemySpawnPoints.Length) break;

                var config = stage.enemiesConfig[i];
                GameObject go = null;

                if (!string.IsNullOrEmpty(config.prefabAddress))
                {
                    try
                    {
                        go = await Addressables.InstantiateAsync(config.prefabAddress, enemySpawnPoints[i].position, enemySpawnPoints[i].rotation).Task;
                    }
                    catch (System.Exception)
                    {
                        go = null;
                    }
                }

                if (go == null && config.prefabVisual != null)
                {
                    go = Instantiate(config.prefabVisual, enemySpawnPoints[i].position, enemySpawnPoints[i].rotation);
                }

                if (go == null)
                {
                    go = new GameObject($"Enemy_{config.name}");
                    go.transform.position = enemySpawnPoints[i].position;
                    go.transform.rotation = enemySpawnPoints[i].rotation;
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

            // 3. Start Combat Manager
            CombatManager.Instance.StartCombat(players, enemies);

            // 4. Open Combat HUD Panel
            UIManager.Instance.OpenPanel("CombatHUD");
        }
    }
}
