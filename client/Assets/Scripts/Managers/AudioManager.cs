using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using GameClient.Core;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using DG.Tweening;

namespace GameClient.Managers
{
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Audio Sources")]
        public AudioSource musicSourceA;
        public AudioSource musicSourceB;
        public AudioSource sfxSourcePrefab;

        [Header("Cấu hình")]
        public float crossFadeDuration = 1.0f;
        public float duckingVolume = 0.2f; // Mức âm lượng khi bị "dìm"
        
        private List<AudioSource> _sfxPool = new List<AudioSource>();
        private bool _isSourceAActive = true;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;
        private bool _isDucking = false;
        private Coroutine _crossFadeCoroutine;

        protected override void Awake()
        {
            base.Awake();
            InitializePool();
            
            EventManager.Instance.AddListener(GameEvents.ON_SETTINGS_CHANGED, _ => ApplySettings());
        }

        private void OnDestroy()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.RemoveListener(GameEvents.ON_SETTINGS_CHANGED, _ => ApplySettings());
            }
        }

        private void InitializePool()
        {
            if (sfxSourcePrefab == null)
            {
                Debug.LogWarning("[AudioManager] sfxSourcePrefab is null. Creating a default one.");
                GameObject go = new GameObject("DefaultSFXSource");
                go.transform.SetParent(transform);
                sfxSourcePrefab = go.AddComponent<AudioSource>();
                go.SetActive(false);
            }

            for (int i = 0; i < 15; i++)
            {
                AudioSource source = Instantiate(sfxSourcePrefab, transform);
                source.gameObject.SetActive(false);
                _sfxPool.Add(source);
            }
        }

        
        public async void PlayMusic(string addressableKey, bool loop = true)
        {
            try
            {
                AudioClip clip = await ResourceManager.Instance.LoadAssetAsync<AudioClip>(addressableKey);
                if (clip != null) 
                {
                    PlayMusic(clip, loop);
                }
                else
                {
                    Debug.LogWarning($"[AudioManager] Cannot load BGM: {addressableKey} (Clip is null)");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AudioManager] Failed to load BGM '{addressableKey}': {ex.Message}");
            }
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;
            
            AudioSource active = _isSourceAActive ? musicSourceA : musicSourceB;
            if (active != null && active.clip == clip && active.isPlaying) 
            {
                return;
            }

            if (_crossFadeCoroutine != null)
            {
                StopCoroutine(_crossFadeCoroutine);
            }
            _crossFadeCoroutine = StartCoroutine(Co_CrossFadeMusic(clip, loop));
        }

        public void StopMusic()
        {
            if (musicSourceA != null) musicSourceA.Stop();
            if (musicSourceB != null) musicSourceB.Stop();
        }

        
        public async void PlaySFX(string addressableKey)
        {
            try
            {
                AudioClip clip = await ResourceManager.Instance.LoadAssetAsync<AudioClip>(addressableKey);
                if (clip != null) PlaySFX(clip);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AudioManager] Cannot load SFX: {addressableKey} -> {ex.Message}");
            }
        }

        public async void PlayLocalizedSFX(string table, string key)
        {
            try
            {
                var handle = UnityEngine.Localization.Settings.LocalizationSettings.AssetDatabase.GetLocalizedAssetAsync<AudioClip>(table, key);
                AudioClip clip = await handle.Task;
                if (clip != null) PlaySFX(clip);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AudioManager] Cannot load Localized SFX: [{table}/{key}] -> {ex.Message}");
            }
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            AudioSource source = GetFreeSFXSource();
            source.PlayOneShot(clip, _sfxVolume);
        }

        public async void PlayRepeatSFX(string addressableKey, int repeatCount, float interval)
        {
            AudioClip clip = await ResourceManager.Instance.LoadAssetAsync<AudioClip>(addressableKey);
            if (clip != null) PlayRepeatSFX(clip, repeatCount, interval);
        }

        public void PlayRepeatSFX(AudioClip clip, int repeatCount, float interval)
        {
            if (clip == null) return;
            StartCoroutine(Co_PlayRepeatSFX(clip, repeatCount, interval));
        }

        public async void PlayExclusiveSFX(string addressableKey)
        {
            AudioClip clip = await ResourceManager.Instance.LoadAssetAsync<AudioClip>(addressableKey);
            if (clip != null) PlayExclusiveSFX(clip);
        }

        public void PlayExclusiveSFX(AudioClip clip)
        {
            if (clip == null) return;
            StartCoroutine(Co_PlayExclusiveSFX(clip));
        }

        public void StopAllSFX()
        {
            foreach (var s in _sfxPool) s.Stop();
        }


        private AudioSource GetFreeSFXSource()
        {
            AudioSource source = _sfxPool.Find(s => !s.isPlaying);
            if (source == null)
            {
                source = Instantiate(sfxSourcePrefab, transform);
                _sfxPool.Add(source);
            }
            source.gameObject.SetActive(true);
            return source;
        }

        private IEnumerator Co_CrossFadeMusic(AudioClip newClip, bool loop)
        {
            if (musicSourceA == null) musicSourceA = gameObject.AddComponent<AudioSource>();
            if (musicSourceB == null) musicSourceB = gameObject.AddComponent<AudioSource>();

            AudioSource active = _isSourceAActive ? musicSourceA : musicSourceB;
            AudioSource next = _isSourceAActive ? musicSourceB : musicSourceA;

            next.clip = newClip;
            next.loop = loop;
            next.volume = 0;
            next.Play();

            float targetVol = _isDucking ? duckingVolume : _musicVolume;
            
            active.DOFade(0, crossFadeDuration).SetEase(Ease.InOutSine);
            yield return next.DOFade(targetVol, crossFadeDuration).SetEase(Ease.InOutSine).WaitForCompletion();

            active.Stop();
            _isSourceAActive = !_isSourceAActive;
        }

        private IEnumerator Co_PlayRepeatSFX(AudioClip clip, int count, float interval)
        {
            for (int i = 0; i < count; i++)
            {
                PlaySFX(clip);
                yield return new WaitForSeconds(interval);
            }
        }

        private IEnumerator Co_PlayExclusiveSFX(AudioClip clip)
        {
            _isDucking = true;
            AudioSource activeMusic = _isSourceAActive ? musicSourceA : musicSourceB;
            
            if (activeMusic != null) activeMusic.DOFade(duckingVolume, 0.5f);

            PlaySFX(clip);
            yield return new WaitForSeconds(clip.length);

            if (activeMusic != null) activeMusic.DOFade(_musicVolume, 0.5f);
            _isDucking = false;
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = volume;
            if (!_isDucking)
            {
                if (musicSourceA != null) musicSourceA.volume = volume;
                if (musicSourceB != null) musicSourceB.volume = volume;
            }
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = volume;
        }

        public void ApplySettings()
        {
            var settings = TFSO.Managers.SettingsManager.Instance.CurrentSettings;
            
            float targetMusicVol = (settings.MasterMute || settings.MusicMute) ? 0 : (settings.MusicVolume * settings.MasterVolume);
            SetMusicVolume(targetMusicVol);

            _sfxVolume = (settings.MasterMute || settings.SfxMute) ? 0 : (settings.SfxVolume * settings.MasterVolume);
        }

        public void SetMusicMute(bool isMute)
        {
            TFSO.Managers.SettingsManager.Instance.CurrentSettings.MusicMute = isMute;
            TFSO.Managers.SettingsManager.Instance.SaveSettings();
            ApplySettings();
        }

        public void SetSFXMute(bool isMute)
        {
            TFSO.Managers.SettingsManager.Instance.CurrentSettings.SfxMute = isMute;
            TFSO.Managers.SettingsManager.Instance.SaveSettings();
            ApplySettings();
        }
    }
}
