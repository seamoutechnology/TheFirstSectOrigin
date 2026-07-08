using UnityEngine;
using System;
using GameClient.Managers;

namespace TFSO.Managers
{
    [Serializable]
    public class GameSettings
    {
        public int LanguageIndex = 0;
        
        [Header("Âm lượng")]
        public float MasterVolume = 1f;
        public float MusicVolume = 1f;
        public float SfxVolume = 1f;
        
        [Header("Bật/Tắt")]
        public bool MasterMute = false;
        public bool MusicMute = false;
        public bool SfxMute = false;

        public int QualityLevel = 2; 

        [Header("Gameplay")]
        public bool EnableSwipeHarvest = true;
    }

    public class SettingsManager : GameClient.Singleton<SettingsManager>
    {

        public GameSettings CurrentSettings { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            LoadSettings();
        }

        public void LoadSettings()
        {
            string json = PlayerPrefs.GetString(GameClient.Core.GameConstants.PlayerPrefsKeys.SETTINGS, "");
            if (string.IsNullOrEmpty(json))
            {
                CurrentSettings = new GameSettings();
                if (GameClient.Core.GameSettings.Instance != null)
                {
                    var def = GameClient.Core.GameSettings.Instance;
                    CurrentSettings.LanguageIndex = def.defaultLocaleCode == "vi" ? 0 : 1;
                    CurrentSettings.MasterVolume = def.defaultMasterVolume;
                    CurrentSettings.MusicVolume = def.defaultMusicVolume;
                    CurrentSettings.SfxVolume = def.defaultSfxVolume;
                    CurrentSettings.MasterMute = def.defaultMasterMute;
                }
            }
            else
            {
                CurrentSettings = JsonUtility.FromJson<GameSettings>(json);
            }
        }

        public void SaveSettings()
        {
            string json = JsonUtility.ToJson(CurrentSettings);
            PlayerPrefs.SetString(GameClient.Core.GameConstants.PlayerPrefsKeys.SETTINGS, json);
            PlayerPrefs.Save();
        }

        public void SetLanguage(int index)
        {
            CurrentSettings.LanguageIndex = index;
            SaveSettings();
            
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SetLanguage(index);
            }
        }
    }
}
