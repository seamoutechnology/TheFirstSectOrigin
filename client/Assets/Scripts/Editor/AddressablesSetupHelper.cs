#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GameClient.Editor
{
    public static class AddressablesSetupHelper
    {
        [MenuItem("Tools/Setup Addressables for Project")]
        public static void SetupAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[AddressablesSetupHelper] AddressableAssetSettings not found! Please open Window > Asset Management > Addressables > Groups first to initialize it.");
                EditorUtility.DisplayDialog("Lỗi", "AddressableAssetSettings không tồn tại! Vui lòng mở Window > Asset Management > Addressables > Groups để Unity khởi tạo Addressables, sau đó chạy lại tool này.", "OK");
                return;
            }

            string prefabPath = "Assets/AssetData/UI/Prefabs/UI_BuildingActionPanel.prefab";
            var guid = AssetDatabase.AssetPathToGUID(prefabPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[AddressablesSetupHelper] Không tìm thấy prefab tại: {prefabPath}");
                EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy prefab tại {prefabPath}. Vui lòng tạo prefab này trước.", "OK");
                return;
            }

            var group = settings.DefaultGroup;
            var entry = settings.CreateOrMoveEntry(guid, group);
            if (entry != null)
            {
                entry.address = "UI_BuildingActionPanel";
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                AssetDatabase.SaveAssets();
                Debug.Log($"[AddressablesSetupHelper] Đã đăng ký Addressable thành công: UI_BuildingActionPanel -> {prefabPath}");
                EditorUtility.DisplayDialog("Thành công", "Đã đăng ký UI_BuildingActionPanel vào Addressables thành công!", "OK");
            }
            else
            {
                Debug.LogError("[AddressablesSetupHelper] Không thể tạo entry trong Addressables.");
            }
        }
    }
}
#endif
