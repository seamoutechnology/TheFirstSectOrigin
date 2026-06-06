using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace GameClient.EditorTools.Graph
{
    public static class LocalizationEditorSync
    {
        private const string DEFAULT_TABLE_COLLECTION_NAME = "UI_System";

        public static List<string> GetAvailableLocales()
        {
            var locales = new List<string>();
            var collection = LocalizationEditorSettings.GetStringTableCollection(DEFAULT_TABLE_COLLECTION_NAME);
            if (collection != null)
            {
                foreach (var table in collection.StringTables)
                {
                    locales.Add(table.LocaleIdentifier.Code);
                }
            }
            return locales;
        }

        public static void PushMultiLangTextToTable(string key, Dictionary<string, string> localizedTexts)
        {
            if (string.IsNullOrEmpty(key)) return;

            var collection = LocalizationEditorSettings.GetStringTableCollection(DEFAULT_TABLE_COLLECTION_NAME);
            if (collection == null)
            {
                Debug.LogWarning($"[LocalizationSync] Không tìm thấy String Table Collection '{DEFAULT_TABLE_COLLECTION_NAME}'.");
                return;
            }

            var sharedTableData = collection.SharedData;
            var entry = sharedTableData.GetEntry(key) ?? sharedTableData.AddKey(key);

            bool dirty = false;

            foreach (var table in collection.StringTables)
            {
                string localeCode = table.LocaleIdentifier.Code;
                if (localizedTexts.TryGetValue(localeCode, out string text))
                {
                    var tableEntry = table.GetEntry(entry.Id);
                    if (tableEntry == null)
                    {
                        table.AddEntry(key, text);
                    }
                    else
                    {
                        tableEntry.Value = text;
                    }
                    EditorUtility.SetDirty(table);
                    dirty = true;
                }
            }

            if (dirty) EditorUtility.SetDirty(sharedTableData);
        }

        public static Dictionary<string, string> PullMultiLangTextFromTable(string key)
        {
            var dict = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(key)) return dict;

            var collection = LocalizationEditorSettings.GetStringTableCollection(DEFAULT_TABLE_COLLECTION_NAME);
            if (collection == null) return dict;

            foreach (var table in collection.StringTables)
            {
                var entry = table.GetEntry(key);
                if (entry != null)
                {
                    dict[table.LocaleIdentifier.Code] = entry.LocalizedValue;
                }
            }
            return dict;
        }
    }
}
