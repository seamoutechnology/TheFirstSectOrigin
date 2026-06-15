using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using GameClient.Managers;
using GameClient.Network;
using GameClient.UI;
using TMPro;

namespace GameClient.Core
{
    [RequireComponent(typeof(ShaderPrecompiler))]
    public class Bootstrap : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private CanvasGroup loadingCanvasGroup; 

        private ShaderPrecompiler _shaderCompiler;

        private async void Start()
        {
            Time.timeScale = 1f; 
            _shaderCompiler = GetComponent<ShaderPrecompiler>();
            progressBar.value = 0f;
            
            await InitializeLocalizationAsync();
            InitializeAudioAndSettings();
            await OptimizeHardwareAsync();
            await CheckUpdatesAsync();
            await OptimizeGraphicsAsync();
            
            bool autoLoginSuccess = await TryAutoLoginProcessAsync();
            if (autoLoginSuccess)
            {
                return;
            }

            await CompleteBootstrapAsync();
            OpenLoginUI();
        }

        private async Task InitializeLocalizationAsync()
        {
            statusText.text = "Tải tệp ngôn ngữ...";
            
            await Addressables.InitializeAsync().Task;
            await UnityEngine.Localization.Settings.LocalizationSettings.InitializationOperation.Task;
            
            var root = GameObject.Find("UIRoot"); 
            if (root != null)
            {
                UIManager.Instance.SetCanvasRoot(root.transform);
            }
            
            int savedLangIndex = TFSO.Managers.SettingsManager.Instance.CurrentSettings.LanguageIndex;
            string defaultLocaleCode = GameClient.Core.GameSettings.Instance.defaultLocaleCode;
            
            string json = PlayerPrefs.GetString(GameClient.Core.GameConstants.PlayerPrefsKeys.SETTINGS, "");
            if (string.IsNullOrEmpty(json))
            {
                await LocalizationManager.Instance.SetLanguageByCodeAsync(defaultLocaleCode);
            }
            else
            {
                await LocalizationManager.Instance.SetLanguageAsync(savedLangIndex);
            }
        }

        private void InitializeAudioAndSettings()
        {
            DG.Tweening.DOTween.SetTweensCapacity(1000, 100);

            AudioManager.Instance.PlayMusic(GameConstants.Audio.BGM_LOBBY, true);
            
            AudioManager.Instance.ApplySettings();

            _ = PlayTimeManager.Instance;
        }

        private async Task OptimizeHardwareAsync()
        {
            statusText.text = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM,"boot_optimizing_hardware");
            HardwareManager.AutoDetectAndApplySettings();
            await Task.Delay(500); 
        }

        private async Task CheckUpdatesAsync()
        {
            statusText.text = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM,"boot_checking_updates");
            await CheckAndDownloadUpdates();
        }

        private async Task OptimizeGraphicsAsync()
        {
            statusText.text = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "boot_optimizing_graphics");
            await CompileShadersAsync();
        }

        private async Task<bool> TryAutoLoginProcessAsync()
        {
            Debug.Log("[Bootstrap] Bắt đầu kiểm tra Auto Login...");
            statusText.text = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "boot_auth_success");
            bool autoLoginSuccess = await TryAutoLogin();
            Debug.Log($"[Bootstrap] Kết quả Auto Login: {autoLoginSuccess}");
            
            if (autoLoginSuccess)
            {
                statusText.text = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_quick_auth_welcome", AccountManager.Instance.CurrentUsername);
                await Task.Delay(500);
                await FadeOutLoadingUI();
                UIManager.Instance.OpenPanel("EntryPanel", null, false);
                return true;
            }
            
            return false;
        }

        private async Task CompleteBootstrapAsync()
        {
            statusText.text = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "boot_complete");
            progressBar.value = 1f;
            await Task.Delay(500);
            await FadeOutLoadingUI();
        }

        private void OpenLoginUI()
        {
            Debug.Log("[Bootstrap] Đang mở LoginPanel...");
            UIManager.Instance.OpenPanel("LoginPanel", null, false);
        }

        private async Task<bool> CheckAndDownloadUpdates()
        {
            try
            {

                var checkForUpdateHandle = Addressables.CheckForCatalogUpdates(false);
                var catalogs = await checkForUpdateHandle.Task;

                if (catalogs.Count > 0)
                {
                    statusText.text = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "boot_loading_resources");
                    
                    var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
                    var locators = await updateHandle.Task;

                    foreach (var locator in locators)
                    {
                        var sizeHandle = Addressables.GetDownloadSizeAsync(locator.Keys);
                        long totalBytes = await sizeHandle.Task;

                        if (totalBytes > 0)
                        {
                            var downloadHandle = Addressables.DownloadDependenciesAsync(locator.Keys, Addressables.MergeMode.Union);
                            
                            while (!downloadHandle.IsDone)
                            {
                                if (!Application.isPlaying || this == null) break;

                                progressBar.value = downloadHandle.PercentComplete;
                                statusText.text = $"Đang tải tài nguyên: {(downloadHandle.PercentComplete * 100):0.0}%";
                                statusText.text = $"boot_loading_percent";
                                await Task.Yield();
                            }
                            if (Application.isPlaying && this != null)
                            {
                                Addressables.Release(downloadHandle);
                            }
                        }
                    }
                    return true;
                }
                
                progressBar.value = 1f;
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Bootstrap] Lỗi update: {ex.Message}");
                return false;
            }
        }

        private Task CompileShadersAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(_shaderCompiler.WarmUpShaders(
                progress => { progressBar.value = progress; },
                () => { tcs.SetResult(true); }
            ));
            return tcs.Task;
        }
        private async Task<bool> TryAutoLogin()
        {
            string token = AccountManager.Instance.CurrentToken;
            if (string.IsNullOrEmpty(token)) return false;

            try
            {
                var loginTask = NetworkManager.Instance.PostAsync<AccountDashboardPanel.UserProfileData>("/api/profile", new { });
                var completedTask = await Task.WhenAny(loginTask, Task.Delay(3000));

                if (completedTask == loginTask)
                {
                    var profile = await loginTask;
                    return profile != null;
                }
                else
                {
                    Debug.LogWarning("[Bootstrap] AutoLogin bị Timeout sau 3s.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Bootstrap] AutoLogin thất bại: {ex.Message}");
                return false;
            }
        }

        private async Task FadeOutLoadingUI()
        {
            float elapsed = 0;
            float duration = 0.6f;
            while (elapsed < duration)
            {
                if (!Application.isPlaying || this == null) return;

                elapsed += Time.deltaTime;
                float alpha = 1 - (elapsed / duration);
                if (loadingCanvasGroup != null) 
                {
                    loadingCanvasGroup.alpha = alpha;
                    loadingCanvasGroup.blocksRaycasts = false;
                    loadingCanvasGroup.interactable = false;
                }
                else
                {
                    if (statusText != null) statusText.alpha = alpha;
                    if (progressBar != null) {
                        var img = progressBar.GetComponentInChildren<Image>();
                        if (img != null) img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
                    }
                }
                await Task.Yield();
            }

            if (loadingCanvasGroup != null) 
            {
                loadingCanvasGroup.gameObject.SetActive(false);
            }
            else
            {
                if (statusText != null) statusText.gameObject.SetActive(false);
                if (progressBar != null) progressBar.gameObject.SetActive(false);
            }
        }
    }
}
