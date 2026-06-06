using UnityEngine;
using System.Collections.Generic;
using GameClient.BaseBuilding.Grid;
using System;

namespace GameClient.BaseBuilding.Core
{
    public class BaseSerializer
    {
        [Serializable]
        private class ExportData
        {
            public string version = "1.0";
            public List<GridManager.PlacedBuildingData> buildings;
        }

        public static string ExportLayoutToString()
        {
            if (GridManager.Instance == null) return string.Empty;

            var data = new ExportData
            {
                buildings = GridManager.Instance.placedBuildings
            };

            string json = JsonUtility.ToJson(data);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            string base64 = Convert.ToBase64String(bytes);

            return "BASE#" + base64;
        }

        public static bool ImportLayoutFromString(string encodedData, Action<GridManager.PlacedBuildingData> onSpawnBuilding)
        {
            if (string.IsNullOrEmpty(encodedData) || !encodedData.StartsWith("BASE#"))
            {
                Debug.LogError("[BaseSerializer] Chuỗi Import không hợp lệ!");
                return false;
            }

            try
            {
                string base64 = encodedData.Substring(5); // Cắt bỏ "BASE#"
                byte[] bytes = Convert.FromBase64String(base64);
                string json = System.Text.Encoding.UTF8.GetString(bytes);

                var data = JsonUtility.FromJson<ExportData>(json);

                if (data != null && data.buildings != null)
                {
                    
                    foreach (var b in data.buildings)
                    {
                        onSpawnBuilding?.Invoke(b);
                        
                        GridManager.Instance.PlaceBuilding(b.buildingId, b.x, b.y, b.width, b.height);
                    }
                    
                    Debug.Log($"[BaseSerializer] Import thành công {data.buildings.Count} tòa nhà.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaseSerializer] Lỗi khi Import: {ex.Message}");
            }

            return false;
        }
    }
}
