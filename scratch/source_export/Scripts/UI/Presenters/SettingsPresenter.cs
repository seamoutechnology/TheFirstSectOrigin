using System;
using UnityEngine;
using GameClient.Managers;
using GameClient.Core;
using TFSO.Managers;
using VContainer;

namespace GameClient.UI.Presenters
{
    public class SettingsPresenter
    {
        private ISettingsView _view;
        private readonly SettingsManager _settingsManager;
        private readonly UIManager _uiManager;
        private readonly EventManager _eventManager;

        [Inject]
        public SettingsPresenter(SettingsManager settingsManager, UIManager uiManager, EventManager eventManager)
        {
            _settingsManager = settingsManager;
            _uiManager = uiManager;
            _eventManager = eventManager;
        }

        public void SetView(ISettingsView view)
        {
            _view = view;
        }

        public void Initialize()
        {
            var settings = _settingsManager.CurrentSettings;

            _view.InitializeSliders(settings.MasterVolume, settings.MusicVolume, settings.SfxVolume);
            _view.InitializeToggles(!settings.MasterMute, !settings.MusicMute, !settings.SfxMute);

            _view.OnMasterVolumeChanged += HandleMasterVolumeChanged;
            _view.OnMusicVolumeChanged += HandleMusicVolumeChanged;
            _view.OnSFXVolumeChanged += HandleSFXVolumeChanged;
            
            _view.OnMasterMuteChanged += HandleMasterMuteChanged;
            _view.OnMusicMuteChanged += HandleMusicMuteChanged;
            _view.OnSFXMuteChanged += HandleSFXMuteChanged;
            
            _view.OnLogoutRequested += HandleLogout;
            _view.OnCloseRequested += HandleClose;
        }

        public void Dispose()
        {
            _view.OnMasterVolumeChanged -= HandleMasterVolumeChanged;
            _view.OnMusicVolumeChanged -= HandleMusicVolumeChanged;
            _view.OnSFXVolumeChanged -= HandleSFXVolumeChanged;
            
            _view.OnMasterMuteChanged -= HandleMasterMuteChanged;
            _view.OnMusicMuteChanged -= HandleMusicMuteChanged;
            _view.OnSFXMuteChanged -= HandleSFXMuteChanged;
            
            _view.OnLogoutRequested -= HandleLogout;
            _view.OnCloseRequested -= HandleClose;
        }

        private void HandleMasterVolumeChanged(float val)
        {
            _settingsManager.CurrentSettings.MasterVolume = val;
            ApplyAndSave();
        }

        private void HandleMusicVolumeChanged(float val)
        {
            _settingsManager.CurrentSettings.MusicVolume = val;
            ApplyAndSave();
        }

        private void HandleSFXVolumeChanged(float val)
        {
            _settingsManager.CurrentSettings.SfxVolume = val;
            ApplyAndSave();
        }

        private void HandleMasterMuteChanged(bool enabled)
        {
            _settingsManager.CurrentSettings.MasterMute = !enabled;
            ApplyAndSave();
        }

        private void HandleMusicMuteChanged(bool enabled)
        {
            _settingsManager.CurrentSettings.MusicMute = !enabled;
            ApplyAndSave();
        }

        private void HandleSFXMuteChanged(bool enabled)
        {
            _settingsManager.CurrentSettings.SfxMute = !enabled;
            ApplyAndSave();
        }

        private void ApplyAndSave()
        {
            _settingsManager.SaveSettings();
            _eventManager.Emit(GameEvents.ON_SETTINGS_CHANGED);
        }

        private void HandleLogout()
        {
            Debug.Log("<color=yellow>[Settings] Đang đăng xuất...</color>");
            
            _eventManager.Emit(GameEvents.ON_LOGOUT);

            PlayerPrefs.DeleteKey(GameConstants.PlayerPrefsKeys.TOKEN);
            PlayerPrefs.DeleteKey(GameConstants.PlayerPrefsKeys.LAST_ACCOUNT);
            PlayerPrefs.Save();

            _uiManager.GoToLogin();
            _view.Hide();
        }

        private void HandleClose()
        {
            _view.Hide();
        }
    }
}
