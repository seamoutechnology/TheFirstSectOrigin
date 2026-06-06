using UnityEngine;
using UnityEditor;
using GameClient.UI;

public class CreateUIBuildPrefabs : EditorWindow
{
    [MenuItem("Tools/Generate BuildMenu UI Prefabs")]
    public static void GeneratePrefabs()
    {
        string dir = "Assets/AssetData/UI";
        if (!AssetDatabase.IsValidFolder(dir))
        {
            AssetDatabase.CreateFolder("Assets", "AssetData");
            AssetDatabase.CreateFolder("Assets/AssetData", "UI");
        }

        GameObject pcObj = new GameObject("BuildMenuPanel_PC");
        var pcCanvas = pcObj.AddComponent<Canvas>();
        pcCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        pcObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        pcObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        var pcPanel = pcObj.AddComponent<BuildMenuPanel>();
        
        string pcPath = $"{dir}/BuildMenuPanel_PC.prefab";
        PrefabUtility.SaveAsPrefabAsset(pcObj, pcPath);
        DestroyImmediate(pcObj);
        
        GameObject mobileObj = new GameObject("BuildMenuPanel_Mobile");
        var mobileCanvas = mobileObj.AddComponent<Canvas>();
        mobileCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mobileObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        mobileObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        var mobilePanel = mobileObj.AddComponent<BuildMenuPanel>();
        
        string mobilePath = $"{dir}/BuildMenuPanel_Mobile.prefab";
        PrefabUtility.SaveAsPrefabAsset(mobileObj, mobilePath);
        DestroyImmediate(mobileObj);
        
        Debug.Log("[BuildMenuPanel] Đã tạo thành công 2 Prefab: BuildMenuPanel_PC và BuildMenuPanel_Mobile tại Assets/AssetData/UI/");
    }
}
