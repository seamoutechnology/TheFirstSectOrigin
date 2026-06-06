using UnityEngine;
using System.Collections;
using GameClient.UI;

namespace GameClient.UI.DebugTools
{
    public class UIDebugger : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            var go = new GameObject("UIDebugger");
            go.AddComponent<UIDebugger>();
            DontDestroyOnLoad(go);
        }

        private bool _hasDumped = false;

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.f12Key.wasPressedThisFrame)
            {
                DumpUIState();
            }

            var panelGo = GameObject.Find("UI_BuildingActionPanel(Clone)");
            if (panelGo != null && panelGo.activeInHierarchy)
            {
                if (!_hasDumped)
                {
                    _hasDumped = true;
                    StartCoroutine(DelayedDump());
                }
            }
            else
            {
                _hasDumped = false;
            }
        }

        private IEnumerator DelayedDump()
        {
            yield return new WaitForSeconds(0.1f);
            DumpUIState();
        }

        public static void DumpUIState()
        {
            Debug.Log("================= UI DEBUGGER DUMP =================");
            var panelGo = GameObject.Find("UI_BuildingActionPanel(Clone)");
            if (panelGo == null)
            {
                // Try searching inactive too
                var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var go in allObjects)
                {
                    if (go.name == "UI_BuildingActionPanel(Clone)")
                    {
                        panelGo = go;
                        break;
                    }
                }
            }

            if (panelGo == null)
            {
                Debug.LogWarning("Không tìm thấy GameObject 'UI_BuildingActionPanel(Clone)' trong scene.");
                return;
            }

            Debug.Log($"Tên: {panelGo.name}");
            Debug.Log($"Active Self: {panelGo.activeSelf}, Active in Hierarchy: {panelGo.activeInHierarchy}");
            
            var canvasGroup = panelGo.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Debug.Log($"CanvasGroup: alpha={canvasGroup.alpha}, interactable={canvasGroup.interactable}, blocksRaycasts={canvasGroup.blocksRaycasts}");
            }
            else
            {
                Debug.LogWarning("Không tìm thấy component CanvasGroup!");
            }

            var rect = panelGo.GetComponent<RectTransform>();
            if (rect != null)
            {
                Debug.Log($"RectTransform: position={rect.position}, localPosition={rect.localPosition}, anchoredPosition={rect.anchoredPosition}");
                Debug.Log($"RectTransform Scale: {rect.localScale}, SizeDelta: {rect.sizeDelta}, AnchorMin: {rect.anchorMin}, AnchorMax: {rect.anchorMax}, Pivot: {rect.pivot}");
            }

            // Print parent hierarchy
            string path = panelGo.name;
            Transform t = panelGo.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                var parentCanvas = t.GetComponent<Canvas>();
                if (parentCanvas != null)
                {
                    Debug.Log($"Parent Canvas tìm thấy ở '{t.name}': renderMode={parentCanvas.renderMode}, sortingOrder={parentCanvas.sortingOrder}, scaleFactor={t.GetComponent<UnityEngine.UI.CanvasScaler>()?.scaleFactor}");
                }
                t = t.parent;
            }
            Debug.Log($"Đường dẫn phân cấp: {path}");

            // Print all child elements and buttons
            Debug.Log("Danh sách các con:");
            foreach (Transform child in panelGo.transform)
            {
                Debug.Log($"  - Con: {child.name}, Active: {child.gameObject.activeSelf}, Position: {child.localPosition}");
                var btn = child.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    Debug.Log($"    * [Button] interactable={btn.interactable}, targetGraphic={btn.targetGraphic?.name}");
                }
            }
            Debug.Log("====================================================");
        }
    }
}
