using System;
using GameClient.Core.Interfaces;
using TFSO.Managers; // Ensure this is available for SettingsData

namespace GameClient.UI.Presenters
{
    public interface ISettingsView : IUIView
    {
        void InitializeSliders(float master, float music, float sfx);
        void InitializeToggles(bool masterMute, bool musicMute, bool sfxMute);

        event Action<float> OnMasterVolumeChanged;
        event Action<float> OnMusicVolumeChanged;
        event Action<float> OnSFXVolumeChanged;

        event Action<bool> OnMasterMuteChanged;
        event Action<bool> OnMusicMuteChanged;
        event Action<bool> OnSFXMuteChanged;

        event Action OnLogoutRequested;
        event Action OnCloseRequested;
    }
}
