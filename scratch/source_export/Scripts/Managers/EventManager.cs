using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameClient.Managers
{
    public class EventManager : Singleton<EventManager>
    {
        private Dictionary<string, Action<object>> _eventChannels = new Dictionary<string, Action<object>>();

        public void AddListener(string eventName, Action<object> listener)
        {
            if (_eventChannels.ContainsKey(eventName))
            {
                _eventChannels[eventName] += listener;
            }
            else
            {
                _eventChannels.Add(eventName, listener);
            }
            
        }

        public void RemoveListener(string eventName, Action<object> listener)
        {
            if (_eventChannels.ContainsKey(eventName))
            {
                _eventChannels[eventName] -= listener;

                if (_eventChannels[eventName] == null || _eventChannels[eventName].GetInvocationList().Length == 0)
                {
                    _eventChannels.Remove(eventName);
                }
            }
        }

        public void Emit(string eventName, object data = null)
        {
            if (_eventChannels.TryGetValue(eventName, out var action))
            {
                action?.Invoke(data);
            }
        }

        public void ClearAllChannels()
        {
            _eventChannels.Clear();
            Debug.Log("[EventManager] Đã làm sạch toàn bộ các kênh sự kiện.");
        }
    }

    public static class GameEvents
    {
        public const string ON_SETTINGS_CHANGED = "OnSettingsChanged";
        public const string ON_VOLUME_CHANGED = "OnVolumeChanged";
        public const string ON_LANGUAGE_CHANGED = "OnLanguageChanged";
        public const string ON_LOGOUT = "OnLogout";
        public const string ON_SERVER_CONNECTED = "OnServerConnected";
        public const string ON_USER_PROFILE_UPDATED = "OnUserProfileUpdated";

        public const string ON_CUTSCENE_STARTED = "OnCutsceneStarted";
        public const string ON_CUTSCENE_STEP_COMPLETED = "OnCutsceneStepCompleted";
        public const string ON_CUTSCENE_FINISHED = "OnCutsceneFinished";
    }
}
