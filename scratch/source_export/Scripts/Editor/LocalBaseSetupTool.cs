#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GameClient.Gameplay.BaseBuilder;
using GameClient.BaseBuilding.Core;

namespace GameClient.Editor
{
    public class LocalBaseSetupTool
    {
        [MenuItem("Tools/Setup LocalBase Scene")]
        public static void SetupLocalBaseScene()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "LocalBase")
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng mở Scene 'LocalBase' trước khi chạy Setup!", "OK");
                return;
            }

            Debug.Log("Đang thiết lập Scene LocalBase...");

            var scopeObj = new GameObject("LocalBaseLifetimeScope");
            scopeObj.AddComponent<LocalBaseLifetimeScope>();

            var bootstrapObj = new GameObject("LocalBaseBootstrap");
            bootstrapObj.AddComponent<LocalBaseBootstrap>();

            var mainCam = Camera.main;
            if (mainCam == null)
            {
                var camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
            }
            if (mainCam.GetComponent<CameraController>() == null)
            {
                mainCam.gameObject.AddComponent<CameraController>();
            }

            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            mainCam.transform.position = new Vector3(0, 10, -10);
            mainCam.transform.rotation = Quaternion.Euler(45, 0, 0);

            var gridObj = GameObject.Find("GridManager");
            if (gridObj == null)
            {
                gridObj = new GameObject("GridManager");
                // TODO: Uncomment when GridManager namespace is confirmed
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("Thiết lập LocalBase thành công! Vui lòng bấm Ctrl+S để lưu Scene.");
        }
    }
}
#endif
