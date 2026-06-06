using UnityEngine;
using UnityEditor;
using GameClient.Gameplay.BaseBuilder;
using System.IO;

public class CreateMainHallAsset
{
    [InitializeOnLoadMethod]
    static void CreateAsset()
    {
        string path = "Assets/Resources/GameData/Buildings/main_hall.asset";
        if (AssetDatabase.LoadAssetAtPath<BuildingData>(path) == null)
        {
            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.BuildingID = "main_hall";
            data.BuildingNameKey = "main_hall";
            data.Type = BuildingType.MainHall;
            data.SizeX = 4;
            data.SizeY = 4;
            data.VisualConfig = AssetDatabase.LoadAssetAtPath<BuildingVisualConfig>("Assets/Resources/GameData/Buildings/main_hall_Visuals.asset");
            
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            Debug.Log("[AutoCreate] Created main_hall.asset");
        }
    }
}
