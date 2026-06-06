#if UNITY_EDITOR
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using GameClient.Cutscenes.Core;
using GameClient.Network;

namespace GameClient.Cutscenes.Editor
{
    [System.Serializable]
    public class CutsceneListResponse
    {
        public List<string> ids;
    }

    public class CutsceneGraphWindow : EditorWindow
    {
        private CutsceneGraphView _graphView;
        private string _fileName = "intro_cutscene";
        private PopupField<string> _dropdown;
        private List<string> _cutsceneIds = new List<string> { "intro_cutscene" };

        [MenuItem("Tools/Cutscene Graph Editor (Cloud)")]
        public static void OpenWindow()
        {
            var window = GetWindow<CutsceneGraphWindow>();
            window.titleContent = new GUIContent("Cutscene Graph");
        }

        private void OnEnable()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            GenerateToolbar();
            ConstructGraphView();
            _ = FetchCutsceneListAsync();
            
            if (EditorPrefs.HasKey("CutsceneGraph_BackupData"))
            {
                string json = EditorPrefs.GetString("CutsceneGraph_BackupData");
                string fName = EditorPrefs.GetString("CutsceneGraph_BackupFileName");
                if (!string.IsNullOrEmpty(json))
                {
                    _fileName = fName;
                    GraphSaveUtility.GetInstance(_graphView).LoadGraphFromJson(json);
                }
                EditorPrefs.DeleteKey("CutsceneGraph_BackupData");
                EditorPrefs.DeleteKey("CutsceneGraph_BackupFileName");
            }
        }

        private void OnDisable()
        {
            if (_graphView != null)
            {
                try
                {
                    string json = GraphSaveUtility.GetInstance(_graphView).ExportGraphToJson(_fileName);
                    EditorPrefs.SetString("CutsceneGraph_BackupData", json);
                    EditorPrefs.SetString("CutsceneGraph_BackupFileName", _fileName ?? "");
                }
                catch { }

                if (rootVisualElement.Contains(_graphView))
                {
                    rootVisualElement.Remove(_graphView);
                }
            }
        }

        private void ConstructGraphView()
        {
            _graphView = new CutsceneGraphView
            {
                name = "Cutscene Graph"
            };
            _graphView.style.flexGrow = 1;
            rootVisualElement.Add(_graphView);
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();
            toolbar.style.flexWrap = Wrap.Wrap;
            toolbar.style.height = StyleKeyword.Auto;
            
            _dropdown = new PopupField<string>("Select Cutscene:", _cutsceneIds, 0);
            _dropdown.RegisterValueChangedCallback(evt => _fileName = evt.newValue);
            toolbar.Add(_dropdown);

            var fileNameTextField = new TextField("New Name:");
            fileNameTextField.RegisterValueChangedCallback(evt => _fileName = evt.newValue);
            toolbar.Add(fileNameTextField);

            toolbar.Add(new Button(() => _ = FetchCutsceneListAsync()) { text = "Refresh List" });
            toolbar.Add(new Button(() => SaveData()) { text = "Save to Server" });
            toolbar.Add(new Button(() => LoadData()) { text = "Load from Server" });
            toolbar.Add(new Button(() => ImportLocalJson()) { text = "Import JSON File" });
            toolbar.Add(new Button(() => ImportFromClipboard()) { text = "Import from Clipboard" });

            toolbar.Add(new Button(() => _graphView.CreateNode("Move", CutsceneNodeType.MoveTo)) { text = "Add Move" });
            toolbar.Add(new Button(() => _graphView.CreateNode("Wait", CutsceneNodeType.Wait)) { text = "Add Wait" });
            toolbar.Add(new Button(() => _graphView.CreateNode("Dialogue", CutsceneNodeType.Dialogue)) { text = "Add Dialogue" });
            toolbar.Add(new Button(() => _graphView.CreateNode("Open UI", CutsceneNodeType.OpenUI)) { text = "Add OpenUI" });
            toolbar.Add(new Button(() => _graphView.CreateNode("Cam Move", CutsceneNodeType.CameraMove)) { text = "Add CamMove" });
            toolbar.Add(new Button(() => _graphView.CreateNode("Cam Shake", CutsceneNodeType.CameraShake)) { text = "Add CamShake" });
            toolbar.Add(new Button(() => _graphView.CreateNode("Play Sound", CutsceneNodeType.PlaySound)) { text = "Add PlaySound" });
            toolbar.Add(new Button(() => _graphView.CreateNode("Parent To", CutsceneNodeType.ParentTo)) { text = "Add ParentTo" });
            toolbar.Add(new Button(() => _graphView.CreateNode("Play Anim", CutsceneNodeType.PlayAnimation)) { text = "Add PlayAnim" });
            toolbar.Add(new Button(() => _graphView.CreateNode("Find Bldg", CutsceneNodeType.FindBuilding)) { text = "Add FindBuilding" });
            toolbar.Add(new Button(() => _graphView.CreateNode("Destroy Entity", CutsceneNodeType.DestroyEntity)) { text = "Add DestroyEntity" });

            rootVisualElement.Add(toolbar);
        }

        private async Task FetchCutsceneListAsync()
        {
            string url = NetworkConfig.BASE_URL + "/api/v1/cutscenes/";
            using (var req = UnityWebRequest.Get(url))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<CutsceneListResponse>(req.downloadHandler.text);
                    if (response != null && response.ids != null && response.ids.Count > 0)
                    {
                        _cutsceneIds.Clear();
                        _cutsceneIds.AddRange(response.ids);
                        _dropdown.choices = _cutsceneIds;
                        if (_cutsceneIds.Contains(_fileName))
                        {
                            _dropdown.value = _fileName;
                        }
                        else
                        {
                            _dropdown.value = _cutsceneIds[0];
                            _fileName = _cutsceneIds[0];
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[Cutscene Editor] Failed to fetch list: {req.error}");
                }
            }
        }

        private void SaveData()
        {
            if (string.IsNullOrEmpty(_fileName))
            {
                EditorUtility.DisplayDialog("Invalid file name", "Please enter a valid file name.", "OK");
                return;
            }
            GraphSaveUtility.GetInstance(_graphView).SaveGraphAsync(_fileName);
        }

        private void LoadData()
        {
            if (string.IsNullOrEmpty(_fileName))
            {
                EditorUtility.DisplayDialog("Invalid file name", "Please enter a valid file name.", "OK");
                return;
            }
            GraphSaveUtility.GetInstance(_graphView).LoadGraphAsync(_fileName);
        }

        private void ImportLocalJson()
        {
            string path = EditorUtility.OpenFilePanel("Select Cutscene JSON", "", "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string json = System.IO.File.ReadAllText(path);
                GraphSaveUtility.GetInstance(_graphView).LoadGraphFromJson(json);
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Import Failed", $"Cannot read file: {ex.Message}", "OK");
            }
        }

        private void ImportFromClipboard()
        {
            try
            {
                string json = EditorGUIUtility.systemCopyBuffer;
                if (string.IsNullOrEmpty(json))
                {
                    EditorUtility.DisplayDialog("Import Failed", "Clipboard is empty.", "OK");
                    return;
                }
                GraphSaveUtility.GetInstance(_graphView).LoadGraphFromJson(json);
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Import Failed", $"Invalid JSON: {ex.Message}", "OK");
            }
        }
    }
}
#endif
