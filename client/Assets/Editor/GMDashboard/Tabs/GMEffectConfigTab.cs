using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace GameClient.Editor.GMDashboard
{
    public class GMEffectConfigTab
    {
        private EditorWindow window;
        private string adminUrl = "http://localhost:8080/api/gm";
        private string AdminUrl => GMDashboardConfig.GmApiUrl;
        private List<GMEffectConfigData> effectList = new List<GMEffectConfigData>();
        private GMEffectConfigData currentEffect = null;
        private bool isEditing = false;
        private Vector2 scrollPos;

        public GMEffectConfigTab(EditorWindow window)
        {
            this.window = window;
        }

        public void OnEnable()
        {
            FetchAllEffects();
        }

        public void OnGUI()
        {
            GUILayout.Label("Quản lý Hiệu ứng (Effect Manager)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            GUI.enabled = GMDashboardConfig.Status == GMDashboardConfig.ConnectionStatus.Online;
            if (GUILayout.Button("Lấy Dữ Liệu (Fetch All)", GUILayout.Height(30)))
            {
                FetchAllEffects();
            }
            if (GUILayout.Button("Thêm Mới (Create New)", GUILayout.Height(30)))
            {
                currentEffect = new GMEffectConfigData() {
                    effect_code = "new_effect",
                    name_key = "New Effect",
                    desc_key = "Description",
                    effect_type = "hp",
                    value_type = "flat"
                };
                isEditing = true;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();

            // cột trái: list
            DrawListPanel();

            // cột phải: detail
            DrawDetailPanel();

            GUILayout.EndHorizontal();
        }

        private void DrawListPanel()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUILayout.Label("Danh sách Effect", EditorStyles.boldLabel);
            
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (var effect in effectList)
            {
                GUILayout.BeginHorizontal("box");
                if (GUILayout.Button(effect.effect_code, EditorStyles.label, GUILayout.ExpandWidth(true)))
                {
                    SelectEffect(effect);
                }
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Xóa Effect", $"Bạn có chắc muốn xóa {effect.effect_code}?", "Xóa", "Hủy"))
                    {
                        DeleteEffect(effect.effect_code);
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawDetailPanel()
        {
            GUILayout.BeginVertical("box");
            
            if (currentEffect == null)
            {
                GUILayout.Label("Chọn 1 Effect bên trái hoặc tạo mới.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label(isEditing ? "Chỉnh sửa Effect" : "Thông tin Effect", EditorStyles.boldLabel);

            GUI.enabled = isEditing;
            currentEffect.effect_code = EditorGUILayout.TextField("Mã Hiệu ứng (Code):", currentEffect.effect_code);
            currentEffect.name_key = EditorGUILayout.TextField("Tên (i18n key):", currentEffect.name_key);
            currentEffect.desc_key = EditorGUILayout.TextField("Mô tả (i18n key):", currentEffect.desc_key);
            
            string[] effectTypes = { 
                "STAT_MODIFIER", 
                "PRODUCTION_MODIFIER", 
                "SECT_BUFF", 
                "GAMEPLAY_MODIFIER" 
            };
            int typeIdx = System.Array.IndexOf(effectTypes, currentEffect.effect_type);
            if (typeIdx < 0) typeIdx = 0;
            typeIdx = EditorGUILayout.Popup("Loại thuộc tính (Type):", typeIdx, effectTypes);
            currentEffect.effect_type = effectTypes[typeIdx];

            string[] valueTypes = { 
                "DIRECT_VALUE", 
                "PERCENT_VALUE", 
                "RANDOM_RANGE_DIRECT", 
                "RANDOM_RANGE_PERCENT", 
                "DEPENDENT_DIRECT", 
                "DEPENDENT_PERCENT" 
            };
            int valIdx = System.Array.IndexOf(valueTypes, currentEffect.value_type);
            if (valIdx < 0) valIdx = 0;
            valIdx = EditorGUILayout.Popup("Cách cộng (Value Type):", valIdx, valueTypes);
            currentEffect.value_type = valueTypes[valIdx];

            currentEffect.min_value = EditorGUILayout.FloatField("Min Value:", currentEffect.min_value);
            currentEffect.max_value = EditorGUILayout.FloatField("Max Value:", currentEffect.max_value);

            if (currentEffect.value_type != null && currentEffect.value_type.StartsWith("DEPENDENT"))
            {
                currentEffect.source_stat = EditorGUILayout.TextField("Chỉ số phụ thuộc (Source Stat):", currentEffect.source_stat);
            }
            else
            {
                currentEffect.source_stat = "";
            }

            GUI.enabled = true;

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            if (isEditing)
            {
                GUI.enabled = GMDashboardConfig.Status == GMDashboardConfig.ConnectionStatus.Online;
                if (GUILayout.Button("Lưu (Save)", GUILayout.Height(30)))
                {
                    SaveEffect();
                }
                GUI.enabled = true;
                if (GUILayout.Button("Hủy (Cancel)", GUILayout.Height(30)))
                {
                    isEditing = false;
                    if (string.IsNullOrEmpty(currentEffect.effect_code))
                    {
                        currentEffect = null;
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Sửa (Edit)", GUILayout.Height(30)))
                {
                    isEditing = true;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void SelectEffect(GMEffectConfigData effect)
        {
            currentEffect = JsonUtility.FromJson<GMEffectConfigData>(JsonUtility.ToJson(effect));
            isEditing = false;
            GUI.FocusControl(null);
        }

        private void FetchAllEffects()
        {
            string url = $"{AdminUrl}/effect_configs";
            var req = UnityWebRequest.Get(url);
            var op = req.SendWebRequest();
            
            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var array = GMJsonHelper.FromJson<GMEffectConfigData>(req.downloadHandler.text);
                    effectList = new List<GMEffectConfigData>(array ?? new GMEffectConfigData[0]);
                    window.Repaint();
                }
            };
        }

        private void SaveEffect()
        {
            if (string.IsNullOrEmpty(currentEffect.effect_code))
            {
                EditorUtility.DisplayDialog("Lỗi", "Mã hiệu ứng không được để trống!", "OK");
                return;
            }

            string url = $"{AdminUrl}/effect_configs/save";
            string json = JsonUtility.ToJson(currentEffect);
            
            var req = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            var op = req.SendWebRequest();
            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    isEditing = false;
                    FetchAllEffects();
                }
                else
                {
                    Debug.LogError($"[GM API] Save Effect Config failed: {req.error}\n{req.downloadHandler.text}");
                }
            };
        }

        private void DeleteEffect(string code)
        {
            string url = $"{AdminUrl}/effect_configs/delete?code={UnityWebRequest.EscapeURL(code)}";
            var req = new UnityWebRequest(url, "POST"); 
            req.downloadHandler = new DownloadHandlerBuffer();
            
            var op = req.SendWebRequest();
            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    if (currentEffect != null && currentEffect.effect_code == code)
                    {
                        currentEffect = null;
                    }
                    FetchAllEffects();
                }
                else
                {
                    Debug.LogError($"[GM API] Delete Effect Config failed: {req.error}\n{req.downloadHandler.text}");
                }
            };
        }
    }
}
