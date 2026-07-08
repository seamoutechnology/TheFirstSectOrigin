#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using GameClient.Cutscenes.Core;
using GameClient.Network;

namespace GameClient.Cutscenes.Editor
{
    public class GraphSaveUtility
    {
        private CutsceneGraphView _targetGraphView;
        
        public static GraphSaveUtility GetInstance(CutsceneGraphView targetGraphView)
        {
            return new GraphSaveUtility
            {
                _targetGraphView = targetGraphView
            };
        }

        public string ExportGraphToJson(string fileName)
        {
            var cutsceneData = new CutsceneGraphData { cutsceneId = fileName };
            var edges = _targetGraphView.edges.ToList();
            var nodes = _targetGraphView.nodes.ToList().Cast<CutsceneNode>().ToList();
            
            var connectedPorts = edges.Where(x => x.input.node != null).ToArray();
            foreach (var edge in connectedPorts)
            {
                var outputNode = edge.output.node as CutsceneNode;
                var inputNode = edge.input.node as CutsceneNode;

                cutsceneData.nodeLinks.Add(new CutsceneNodeLinkData
                {
                    baseNodeGuid = outputNode.GUID,
                    portName = edge.output.portName,
                    targetNodeGuid = inputNode.GUID
                });
            }

            foreach (var cutsceneNode in nodes)
            {
                cutsceneData.nodes.Add(new CutsceneNodeData
                {
                    guid = cutsceneNode.GUID,
                    type = cutsceneNode.NodeType,
                    position = cutsceneNode.GetPosition().position,
                    targetEntityId = cutsceneNode.TargetEntityId,
                    targetPos = cutsceneNode.TargetPos,
                    duration = cutsceneNode.Duration,
                    easeType = cutsceneNode.EaseType,
                    dialogueTable = cutsceneNode.DialogueTable,
                    dialogueKey = cutsceneNode.DialogueKey,
                    panelName = cutsceneNode.PanelName,
                    isLoadByPlatform = cutsceneNode.IsLoadByPlatform,
                    isCameraMoveToEntity = cutsceneNode.IsCameraMoveToEntity,
                    cameraZoom = cutsceneNode.CameraZoom,
                    shakeStrength = cutsceneNode.ShakeStrength,
                    shakeVibrato = cutsceneNode.ShakeVibrato,
                    audioTable = cutsceneNode.AudioTable,
                    audioKey = cutsceneNode.AudioKey,
                    parentEntityId = cutsceneNode.ParentEntityId,
                    animationName = cutsceneNode.AnimationName
                });
            }
            
            HashSet<string> uniqueEntityIds = new HashSet<string>();
            HashSet<string> foundEntityIds = new HashSet<string>();

            foreach (var cutsceneNode in nodes)
            {
                if (cutsceneNode.NodeType == CutsceneNodeType.FindBuilding)
                {
                    if (!string.IsNullOrEmpty(cutsceneNode.TargetEntityId))
                        foundEntityIds.Add(cutsceneNode.TargetEntityId);
                }
                else
                {
                    if (!string.IsNullOrEmpty(cutsceneNode.TargetEntityId))
                        uniqueEntityIds.Add(cutsceneNode.TargetEntityId);
                    if (!string.IsNullOrEmpty(cutsceneNode.ParentEntityId))
                        uniqueEntityIds.Add(cutsceneNode.ParentEntityId);
                }
            }

            uniqueEntityIds.ExceptWith(foundEntityIds);

            foreach (var entityId in uniqueEntityIds)
            {
                if (entityId == "MockBird") cutsceneData.entities.Add(new CutsceneEntityData { id = "MockBird", prefabPath = "Primitive:Sphere", startPos = new Vector3(-10, 5, 0) });
                else if (entityId == "MockCharacter") cutsceneData.entities.Add(new CutsceneEntityData { id = "MockCharacter", prefabPath = "Primitive:Capsule", startPos = Vector3.zero });
                else if (entityId == "MockGate") cutsceneData.entities.Add(new CutsceneEntityData { id = "MockGate", prefabPath = "Primitive:Cube", startPos = new Vector3(5, 0, 0) });
                else
                {
                    cutsceneData.entities.Add(new CutsceneEntityData { id = entityId, prefabPath = entityId, startPos = Vector3.zero });
                }
            }
            
            return JsonUtility.ToJson(cutsceneData, true);
        }

        public async void SaveGraphAsync(string fileName)
        {
            string json = ExportGraphToJson(fileName);
            string url = NetworkConfig.BASE_URL + $"/api/v1/cutscenes/{fileName}.json";
            
            var req = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result == UnityWebRequest.Result.Success)
            {
                EditorUtility.DisplayDialog("Save Success", $"Cutscene {fileName} saved to Server!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Save Failed", $"Error: {req.error}", "OK");
            }
            
            req.Dispose();
        }

        public async void LoadGraphAsync(string fileName)
        {
            string url = NetworkConfig.BASE_URL + $"/api/v1/cutscenes/{fileName}.json";
            using (var req = UnityWebRequest.Get(url))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    EditorUtility.DisplayDialog("Load Failed", $"Error fetching {fileName}: {req.error}", "OK");
                    return;
                }

                CutsceneGraphData cutsceneData = JsonUtility.FromJson<CutsceneGraphData>(req.downloadHandler.text);
                if (cutsceneData == null)
                {
                    EditorUtility.DisplayDialog("Load Failed", "Invalid JSON from server", "OK");
                    return;
                }

                ClearGraph();
                CreateNodes(cutsceneData);
                ConnectNodes(cutsceneData);
            }
        }

        public void LoadGraphFromJson(string json)
        {
            CutsceneGraphData cutsceneData = JsonUtility.FromJson<CutsceneGraphData>(json);
            if (cutsceneData == null || string.IsNullOrEmpty(cutsceneData.cutsceneId))
            {
                EditorUtility.DisplayDialog("Load Failed", "Invalid JSON data", "OK");
                return;
            }
            ClearGraph();
            CreateNodes(cutsceneData);
            ConnectNodes(cutsceneData);
        }

        private void ClearGraph()
        {
            var nodes = _targetGraphView.nodes.ToList().Cast<CutsceneNode>().ToList();
            var edges = _targetGraphView.edges.ToList();
            
            foreach (var node in nodes)
            {
                if (node.EntryPoint) continue;
                _targetGraphView.RemoveElement(node);
            }

            foreach (var edge in edges)
            {
                _targetGraphView.RemoveElement(edge);
            }
        }

        private void CreateNodes(CutsceneGraphData cutsceneData)
        {
            foreach (var nodeData in cutsceneData.nodes)
            {
                if (nodeData.type == CutsceneNodeType.Entry)
                {
                    var entryNode = _targetGraphView.nodes.ToList().Cast<CutsceneNode>().First(n => n.EntryPoint);
                    entryNode.GUID = nodeData.guid;
                    entryNode.SetPosition(new Rect(nodeData.position, _targetGraphView.DefaultNodeSize));
                }
                else
                {
                    var node = new CutsceneNode
                    {
                        title = nodeData.type.ToString(),
                        GUID = nodeData.guid,
                        NodeType = nodeData.type,
                        TargetEntityId = nodeData.targetEntityId,
                        TargetPos = nodeData.targetPos,
                        Duration = nodeData.duration,
                        EaseType = nodeData.easeType,
                        DialogueTable = nodeData.dialogueTable,
                        DialogueKey = nodeData.dialogueKey,
                        PanelName = nodeData.panelName,
                        IsLoadByPlatform = nodeData.isLoadByPlatform,
                        IsCameraMoveToEntity = nodeData.isCameraMoveToEntity,
                        CameraZoom = nodeData.cameraZoom,
                        ShakeStrength = nodeData.shakeStrength > 0 ? nodeData.shakeStrength : 1f,
                        ShakeVibrato = nodeData.shakeVibrato > 0 ? nodeData.shakeVibrato : 10,
                        AudioTable = nodeData.audioTable,
                        AudioKey = nodeData.audioKey,
                        ParentEntityId = nodeData.parentEntityId,
                        AnimationName = nodeData.animationName
                    };

                    var inputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
                    inputPort.portName = "Input";
                    node.inputContainer.Add(inputPort);

                    var outputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
                    outputPort.portName = "Next";
                    node.outputContainer.Add(outputPort);

                    node.AddCustomFields();
                    node.RefreshExpandedState();
                    node.RefreshPorts();
                    node.SetPosition(new Rect(nodeData.position, _targetGraphView.DefaultNodeSize));

                    _targetGraphView.AddElement(node);
                }
            }
        }

        private void ConnectNodes(CutsceneGraphData cutsceneData)
        {
            var allNodes = _targetGraphView.nodes.ToList().Cast<CutsceneNode>().ToList();
            
            foreach (var link in cutsceneData.nodeLinks)
            {
                var outputNode = allNodes.FirstOrDefault(n => n.GUID == link.baseNodeGuid);
                var inputNode = allNodes.FirstOrDefault(n => n.GUID == link.targetNodeGuid);
                if (outputNode == null || inputNode == null) continue;

                var outputPort = outputNode.outputContainer.Children().OfType<Port>().First(p => p.portName == link.portName);
                var inputPort = inputNode.inputContainer.Children().OfType<Port>().First(p => p.direction == Direction.Input);

                var edge = outputPort.ConnectTo(inputPort);
                _targetGraphView.AddElement(edge);
            }
        }
    }
}
#endif
