using System;
using UnityEngine;
using GameClient.Core;

namespace GameClient.Managers
{
    public class PlayTimeManager : Singleton<PlayTimeManager>
    {
        private const string PREFS_PLAY_TIME = "AccumulatedPlayTimeSeconds";
        private const string PREFS_LAST_DATE = "LastPlayDate";

        private float _accumulatedSeconds = 0f;
        private string _lastSavedDate = "";
        private float _saveTimer = 0f;
        private const float SAVE_INTERVAL = 30f; // Sao lưu mỗi 30 giây

        protected override void Awake()
        {
            base.Awake();
            LoadPlayTime();
        }

        private void LoadPlayTime()
        {
            _lastSavedDate = PlayerPrefs.GetString(PREFS_LAST_DATE, "");
            string todayDate = GetTodayDateString();

            if (_lastSavedDate == todayDate)
            {
                _accumulatedSeconds = PlayerPrefs.GetFloat(PREFS_PLAY_TIME, 0f);
            }
            else
            {
                _accumulatedSeconds = 0f;
                _lastSavedDate = todayDate;
                SavePlayTime();
            }
        }

        private void SavePlayTime()
        {
            PlayerPrefs.SetFloat(PREFS_PLAY_TIME, _accumulatedSeconds);
            PlayerPrefs.SetString(PREFS_LAST_DATE, _lastSavedDate);
            PlayerPrefs.Save();
        }

        private void Update()
        {
            _accumulatedSeconds += Time.unscaledDeltaTime;

            string todayDate = GetTodayDateString();
            if (todayDate != _lastSavedDate)
            {
                _accumulatedSeconds = 0f;
                _lastSavedDate = todayDate;
                SavePlayTime();
            }

            _saveTimer += Time.unscaledDeltaTime;
            if (_saveTimer >= SAVE_INTERVAL)
            {
                _saveTimer = 0f;
                SavePlayTime();
            }

            CheckPlayTimeWarning();
        }

        private bool _hasWarned180Mins = false;

        private void CheckPlayTimeWarning()
        {
            if (!_hasWarned180Mins && GetTodayPlayTimeMinutes() >= 180)
            {
                _hasWarned180Mins = true;
                string warnText = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, GameConstants.Locales.WARN_180_MINS);
                if (string.IsNullOrEmpty(warnText)) warnText = "You have played for 180 minutes. Please take a rest for your health.";
                
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowMessage("18+ Warning", warnText);
                }
                else
                {
                    Debug.LogWarning("[180 Mins Warning] " + warnText);
                }
            }
        }

        private void OnApplicationQuit()
        {
            SavePlayTime();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SavePlayTime();
            }
        }

        private string GetTodayDateString()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }

        public int GetTodayPlayTimeMinutes()
        {
            return Mathf.FloorToInt(_accumulatedSeconds / 60f);
        }
    }
}
