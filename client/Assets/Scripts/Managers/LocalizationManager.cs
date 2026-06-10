using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections;
using System.Threading.Tasks;
using GameClient.Core;

namespace GameClient.Managers
{
    public class LocalizationManager : Singleton<LocalizationManager>
    {
        [Header("Cấu hình")]
        public string defaultTable = "UI_System";

        public string GetText(string key, params object[] args)
        {
            return GetText(defaultTable, key, args);
        }

        public string GetText(string table, string key)
        {
            return GetText(table, key, System.Array.Empty<object>());
        }

        public string GetText(string table, string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (string.IsNullOrEmpty(table)) table = defaultTable;

            string translated = "";
            try 
            {
                translated = LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
            }
            catch
            {
                return $"[{table}/{key}]";
            }

            if (string.IsNullOrEmpty(translated))
                return $"[{table}/{key}]";

            try {
                return args.Length > 0 ? string.Format(translated, args) : translated;
            } catch {
                return translated;
            }
        }

        public void LoadLanguage(string jsonContent) { }

        public void SetLanguage(int index)
        {
            StartCoroutine(SetLocale(index));
        }

        public async Task SetLanguageAsync(int index)
        {
            await LocalizationSettings.InitializationOperation.Task;
            if (index >= 0 && index < LocalizationSettings.AvailableLocales.Locales.Count)
            {
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
                
                var op = LocalizationSettings.InitializationOperation;
                await op.Task;

                EventManager.Instance.Emit(GameEvents.ON_LANGUAGE_CHANGED, index);
            }
        }

        public async Task SetLanguageByCodeAsync(string code)
        {
            await LocalizationSettings.InitializationOperation.Task;
            var locales = LocalizationSettings.AvailableLocales.Locales;
            int index = locales.FindIndex(l => l.Identifier.Code == code);
            
            if (index >= 0)
            {
                await SetLanguageAsync(index);
                TFSO.Managers.SettingsManager.Instance.CurrentSettings.LanguageIndex = index;
                TFSO.Managers.SettingsManager.Instance.SaveSettings();
            }
            else
            {
                if (locales.Count > 0)
                {
                    await SetLanguageAsync(0);
                }
            }
        }

        private IEnumerator SetLocale(int index)
        {
            yield return LocalizationSettings.InitializationOperation;
            if (index >= 0 && index < LocalizationSettings.AvailableLocales.Locales.Count)
            {
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
                
                EventManager.Instance.Emit(GameEvents.ON_LANGUAGE_CHANGED, index);
            }
        }

        public T GetAsset<T>(string table, string key) where T : Object
        {
            var handle = LocalizationSettings.AssetDatabase.GetLocalizedAssetAsync<T>(table, key);
            return handle.WaitForCompletion();
        }
    }
}
