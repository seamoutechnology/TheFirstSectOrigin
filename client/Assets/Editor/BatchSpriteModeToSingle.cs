using UnityEngine;
using UnityEditor;
using System.IO;

namespace GameClient.Editor
{
    public class BatchSpriteModeToSingle
    {
        [MenuItem("Tools/Sprites/Convert Selected to Sprite Mode Single", false, 100)]
        private static void ConvertToSingle()
        {
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                Debug.LogWarning("[BatchSpriteMode] Hãy chọn ít nhất một hình ảnh trong cửa sổ Project.");
                return;
            }

            int count = 0;
            foreach (Object obj in selectedObjects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;

                // Chỉ xử lý các file có định dạng hình ảnh thông thường
                string ext = Path.GetExtension(path).ToLower();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" || ext == ".psd" || ext == ".tiff")
                {
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null)
                    {
                        // Kiểm tra xem hình ảnh có đang ở định dạng Sprite và không phải Single không
                        if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode != SpriteImportMode.Single)
                        {
                            importer.spriteImportMode = SpriteImportMode.Single;
                            importer.SaveAndReimport();
                            count++;
                        }
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[BatchSpriteMode] Đã chuyển đổi thành công {count} hình ảnh sang Sprite Mode: Single.");
            EditorUtility.DisplayDialog("Thành công", $"Đã chuyển đổi thành công {count} hình ảnh sang Sprite Mode: Single.", "OK");
        }

        // Validate menu item để chỉ kích hoạt khi có chọn vật phẩm
        [MenuItem("Tools/Sprites/Convert Selected to Sprite Mode Single", true)]
        private static bool ValidateConvertToSingle()
        {
            return Selection.objects != null && Selection.objects.Length > 0;
        }
    }
}
