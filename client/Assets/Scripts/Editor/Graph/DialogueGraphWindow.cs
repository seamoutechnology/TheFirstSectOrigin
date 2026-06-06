using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace GameClient.EditorTools.Graph
{
    public class DialogueGraphWindow : EditorWindow
    {
        private DialogueGraphView _graphView;

        [MenuItem("Tools/Dialogue/Visual Node Editor")]
        public static void OpenDialogueGraphWindow()
        {
            var window = GetWindow<DialogueGraphWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
        }

        private void OnDisable()
        {
            if (_graphView != null)
            {
                rootVisualElement.Remove(_graphView);
            }
        }

        private void ConstructGraphView()
        {
            _graphView = new DialogueGraphView
            {
                name = "Dialogue Graph"
            };

            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        private TextField _sequenceIdField;

        private void GenerateToolbar()
        {
            var toolbar = new UnityEditor.UIElements.Toolbar();

            _sequenceIdField = new TextField("Sequence ID:");
            _sequenceIdField.value = "tutorial_01";
            toolbar.Add(_sequenceIdField);

            var btnCreateNode = new Button(() => { _graphView.CreateNode("Dialogue Node", Vector2.zero); });
            btnCreateNode.text = "Create Node";
            toolbar.Add(btnCreateNode);

            var btnSave = new Button(() => { SaveData(); });
            btnSave.text = "Save to JSON";
            toolbar.Add(btnSave);

            var btnLoad = new Button(() => { LoadData(); });
            btnLoad.text = "Load from JSON";
            toolbar.Add(btnLoad);

            rootVisualElement.Add(toolbar);
        }

        private void SaveData()
        {
            if (_graphView == null) return;

            var nodes = _graphView.nodes.ToList();
            if (nodes.Count == 0) return;

            string seqId = _sequenceIdField.value;
            if (string.IsNullOrEmpty(seqId)) seqId = "sequence_" + System.DateTime.Now.ToString("yyyyMMddHHmmss");

            var dialogueSequence = new GameClient.Dialogue.DialogueSequence
            {
                sequenceId = seqId,
                entryNodeId = ""
            };

            var db = new GameClient.Dialogue.DialogueDatabase();
            var cache = new EditorGraphCache();
            
            int nodeIndex = 1;
            foreach (var node in nodes)
            {
                if (node is DialogueNodeView dialogueNodeView)
                {
                    cache.nodePositions.Add(new EditorNodePosition
                    {
                        nodeId = dialogueNodeView.GUID,
                        position = dialogueNodeView.GetPosition().position
                    });

                    string textKey = $"{seqId}_n{nodeIndex}_{dialogueNodeView.AvatarId}";
                    LocalizationEditorSync.PushMultiLangTextToTable(textKey, dialogueNodeView.LocalizedTexts);
                    
                    var runtimeNode = new GameClient.Dialogue.DialogueNode
                    {
                        nodeId = dialogueNodeView.GUID,
                        textKey = textKey,
                        speakerNameKey = dialogueNodeView.SpeakerKey,
                        avatarId = dialogueNodeView.AvatarId,
                        position = dialogueNodeView.Position
                    };

                    if (string.IsNullOrEmpty(dialogueSequence.entryNodeId))
                    {
                        dialogueSequence.entryNodeId = runtimeNode.nodeId;
                    }

                    var outputPorts = dialogueNodeView.outputContainer.Query<UnityEditor.Experimental.GraphView.Port>().ToList();
                    int choiceIndex = 1;
                    foreach (var port in outputPorts)
                    {
                        foreach (var edge in port.connections)
                        {
                            if (edge.input.node is DialogueNodeView targetNode)
                            {
                                if (port.portName == "Next")
                                {
                                    runtimeNode.nextNodeId = targetNode.GUID;
                                }
                                else if (port.portName.StartsWith("Choice"))
                                {
                                    string choiceTextKey = $"{seqId}_n{nodeIndex}_c{choiceIndex}";
                                    var choiceDict = new Dictionary<string, string> { { "vi", port.portName }, { "en", port.portName } };
                                    LocalizationEditorSync.PushMultiLangTextToTable(choiceTextKey, choiceDict);

                                    runtimeNode.choices.Add(new GameClient.Dialogue.DialogueChoice
                                    {
                                        textKey = choiceTextKey,
                                        nextNodeId = targetNode.GUID
                                    });
                                    choiceIndex++;
                                }
                            }
                        }
                    }

                    dialogueSequence.nodes.Add(runtimeNode);
                    nodeIndex++;
                }
            }

            db.sequences.Add(dialogueSequence);

            System.IO.Directory.CreateDirectory("Assets/Resources/Data");

            string json = JsonUtility.ToJson(db, true);
            string path = "Assets/Resources/Data/DialogueDatabase.json";
            System.IO.File.WriteAllText(path, json, System.Text.Encoding.UTF8);

            string cacheJson = JsonUtility.ToJson(cache, true);
            string cachePath = "Assets/Resources/Data/EditorGraphCache.json";
            System.IO.File.WriteAllText(cachePath, cacheJson, System.Text.Encoding.UTF8);
            
            AssetDatabase.Refresh();
            Debug.Log($"[DialogueGraph] Đã lưu thành công Sequence '{seqId}' ({dialogueSequence.nodes.Count} nodes) - Hỗ trợ Semantic Key đa ngôn ngữ!");
        }

        private void LoadData()
        {
            string path = "Assets/Resources/Data/DialogueDatabase.json";
            string cachePath = "Assets/Resources/Data/EditorGraphCache.json";

            if (!System.IO.File.Exists(path))
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy file DialogueDatabase.json", "OK");
                return;
            }

            var elements = _graphView.graphElements.ToList();
            _graphView.DeleteElements(elements);

            var dbJson = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
            var db = JsonUtility.FromJson<GameClient.Dialogue.DialogueDatabase>(dbJson);

            EditorGraphCache cache = new EditorGraphCache();
            if (System.IO.File.Exists(cachePath))
            {
                var cacheJson = System.IO.File.ReadAllText(cachePath, System.Text.Encoding.UTF8);
                cache = JsonUtility.FromJson<EditorGraphCache>(cacheJson);
            }

            if (db.sequences.Count == 0) return;

            var sequence = db.sequences[0];
            _sequenceIdField.value = sequence.sequenceId;

            var createdNodes = new System.Collections.Generic.Dictionary<string, DialogueNodeView>();

            foreach (var nodeData in sequence.nodes)
            {
                var cachePos = cache.nodePositions.Find(x => x.nodeId == nodeData.nodeId);
                Vector2 pos = cachePos != null ? cachePos.position : Vector2.zero;

                var multiLangDict = LocalizationEditorSync.PullMultiLangTextFromTable(nodeData.textKey);
                var nodeView = _graphView.CreateNode("Dialogue Node", pos, multiLangDict);
                nodeView.GUID = nodeData.nodeId; 
                
                nodeView.SpeakerKey = nodeData.speakerNameKey;
                nodeView.AvatarId = nodeData.avatarId;
                nodeView.Position = nodeData.position;

                createdNodes[nodeData.nodeId] = nodeView;
            }

            foreach (var nodeData in sequence.nodes)
            {
                if (!createdNodes.TryGetValue(nodeData.nodeId, out var sourceNode)) continue;

                if (!string.IsNullOrEmpty(nodeData.nextNodeId) && createdNodes.TryGetValue(nodeData.nextNodeId, out var nextTargetNode))
                {
                    var nextPort = sourceNode.outputContainer.Query<UnityEditor.Experimental.GraphView.Port>().Where(p => p.portName == "Next").First();
                    var targetInputPort = nextTargetNode.inputContainer.Query<UnityEditor.Experimental.GraphView.Port>().First();
                    LinkPorts(nextPort, targetInputPort);
                }

                foreach (var choice in nodeData.choices)
                {
                    if (!string.IsNullOrEmpty(choice.nextNodeId) && createdNodes.TryGetValue(choice.nextNodeId, out var choiceTargetNode))
                    {
                        _graphView.AddChoicePort(sourceNode, choice.textKey); // Tạm dùng textKey làm tên hiển thị
                        var choicePorts = sourceNode.outputContainer.Query<UnityEditor.Experimental.GraphView.Port>().ToList();
                        var choicePort = choicePorts[choicePorts.Count - 1]; // Lấy cổng vừa tạo
                        
                        var targetInputPort = choiceTargetNode.inputContainer.Query<UnityEditor.Experimental.GraphView.Port>().First();
                        LinkPorts(choicePort, targetInputPort);
                    }
                }
            }
            
            Debug.Log("[DialogueGraph] Đã Load thành công dữ liệu Nodes và Edges từ JSON!");
        }

        private void LinkPorts(UnityEditor.Experimental.GraphView.Port outputPort, UnityEditor.Experimental.GraphView.Port inputPort)
        {
            var edge = new UnityEditor.Experimental.GraphView.Edge
            {
                output = outputPort,
                input = inputPort
            };
            edge.input.Connect(edge);
            edge.output.Connect(edge);
            _graphView.AddElement(edge);
        }
    }
}
