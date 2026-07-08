using GameClient.Core;
using UnityEngine;
using UnityEngine.UI;
using GameClient.Core.Interfaces;
using GameClient.UI.Presenters;
using System;
using VContainer;

namespace GameClient.UI
{
    public class SettingsPanel : MonoBehaviour, ISettingsView
    {
        [Header("Audio Sliders")]
        public Slider sliderMaster;
        public Slider sliderMusic;
        public Slider sliderSFX;

        [Header("Toggles")]
        public Toggle toggleMasterMute;
        public Toggle toggleMusicMute;
        public Toggle toggleSFXMute;

        [Header("Buttons")]
        public Button btnLogout;
        public Button btnClose;

        private SettingsPresenter _presenter;

        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;

        public event Action<bool> OnMasterMuteChanged;
        public event Action<bool> OnMusicMuteChanged;
        public event Action<bool> OnSFXMuteChanged;

        public event Action OnLogoutRequested;
        public event Action OnCloseRequested;

        public bool IsVisible => gameObject.activeSelf;

        [Inject]
        public void Construct(SettingsPresenter presenter)
        {
            _presenter = presenter;
            _presenter.SetView(this);
        }

        private void Start()
        {
            sliderMaster?.onValueChanged.AddListener(val => OnMasterVolumeChanged?.Invoke(val));
            sliderMusic?.onValueChanged.AddListener(val => OnMusicVolumeChanged?.Invoke(val));
            sliderSFX?.onValueChanged.AddListener(val => OnSFXVolumeChanged?.Invoke(val));

            toggleMasterMute?.onValueChanged.AddListener(on => OnMasterMuteChanged?.Invoke(on));
            toggleMusicMute?.onValueChanged.AddListener(on => OnMusicMuteChanged?.Invoke(on));
            toggleSFXMute?.onValueChanged.AddListener(on => OnSFXMuteChanged?.Invoke(on));

            if (btnLogout != null)
            {
                btnLogout.onClick.RemoveAllListeners();
                btnLogout.onClick.AddListener(() => OnLogoutRequested?.Invoke());
            }

            if (btnClose != null)
            {
                btnClose.onClick.RemoveAllListeners();
                btnClose.onClick.AddListener(() => OnCloseRequested?.Invoke());
            }
        }

        public void Setup(object data = null)
        {
            if (_presenter == null)
            {
                Debug.LogError("[SettingsPanel] Presenter is null! Make sure to bind SettingsPresenter in GameLifetimeScope.");
                return;
            }

            _presenter.Initialize();
        }

        public void InitializeSliders(float master, float music, float sfx)
        {
            if (sliderMaster != null) sliderMaster.SetValueWithoutNotify(master);
            if (sliderMusic != null) sliderMusic.SetValueWithoutNotify(music);
            if (sliderSFX != null) sliderSFX.SetValueWithoutNotify(sfx);
        }

        public void InitializeToggles(bool masterMute, bool musicMute, bool sfxMute)
        {
            if (toggleMasterMute != null) toggleMasterMute.SetIsOnWithoutNotify(masterMute);
            if (toggleMusicMute != null) toggleMusicMute.SetIsOnWithoutNotify(musicMute);
            if (toggleSFXMute != null) toggleSFXMute.SetIsOnWithoutNotify(sfxMute);
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide()
        {
            gameObject.SetActive(false);
            if (_presenter != null)
            {
                _presenter.Dispose();
            }
        }
    }
}
