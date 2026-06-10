using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using GameClient.Gameplay.Heroes;

namespace GameClient.Managers
{
    public class HeroDataManager : Singleton<HeroDataManager>
    {
        private Dictionary<long, HeroConfig> _heroConfigs = new Dictionary<long, HeroConfig>();
        private bool _isLoaded = false;

        protected override void Awake()
        {
            base.Awake();
            _ = LoadAllHeroesAsync();
        }

        public async Task LoadAllHeroesAsync()
        {
            if (_isLoaded) return;

            try
            {
                var handle = Addressables.LoadAssetsAsync<HeroConfig>("HeroConfig", null);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _heroConfigs.Clear();
                    foreach (var config in handle.Result)
                    {
                        _heroConfigs[config.heroId] = config;
                        Debug.Log($"[HeroDataManager] Loaded Hero: {config.heroId} - {config.heroName}");
                    }
                    _isLoaded = true;
                }
                else
                {
                    Debug.LogError("[HeroDataManager] Failed to load Hero configs from Addressables.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[HeroDataManager] Có thể bạn chưa gán label 'HeroConfig' trong Addressables: {ex.Message}");
                
                var configs = Resources.LoadAll<HeroConfig>("GameData/Heroes");
                if (configs != null && configs.Length > 0)
                {
                    _heroConfigs.Clear();
                    foreach (var config in configs)
                    {
                        _heroConfigs[config.heroId] = config;
                    }
                    _isLoaded = true;
                    Debug.Log($"[HeroDataManager] Fallback Loaded {configs.Length} Heroes from Resources.");
                }
            }
        }

        public HeroConfig GetHeroConfig(long heroId)
        {
            if (_heroConfigs.TryGetValue(heroId, out var config))
            {
                return config;
            }
            Debug.LogWarning($"[HeroDataManager] Không tìm thấy Config cho HeroID: {heroId}");
            return null;
        }

        public HeroConfig GetHeroConfigByName(string name)
        {
            foreach (var config in _heroConfigs.Values)
            {
                if (config.heroName == name)
                {
                    return config;
                }
            }
            return null;
        }

        public HeroConfig GetHeroConfigByCode(string code)
        {
            foreach (var config in _heroConfigs.Values)
            {
                if (config.code == code)
                {
                    return config;
                }
            }
            return null;
        }

        public HeroConfig GetHeroConfigByCodeOrName(string key)
        {
            foreach (var config in _heroConfigs.Values)
            {
                if (config.code == key || config.heroName == key)
                {
                    return config;
                }
            }
            return null;
        }
    }
}
