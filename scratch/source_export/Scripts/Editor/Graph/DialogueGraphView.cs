using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameClient.EditorTools.Graph
{
    public class DialogueNodeView : Node
    {
        public string GUID;
        public Dictionary<string, string> LocalizedTexts = new Dictionary<string, string>();
        public string SpeakerKey;
        public string AvatarId;
        public GameClient.Dialogue.DialoguePosition Position;
    }

    public class DialogueGraphView : GraphView
    {
        public DialogueGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            ports.ForEach(funcCall: port =>
            {
                if (startPort != port && startPort.node != port.node)
                {
                    compatiblePorts.Add(port);
                }
            });
            return compatiblePorts;
        }

        public DialogueNodeView CreateNode(string nodeName, Vector2 position, Dictionary<string, string> existingTexts = null)
        {
            var dialogueNode = new DialogueNodeView
            {
                title = nodeName,
                GUID = Guid.NewGuid().ToString(),
                SpeakerKey = "Tên người nói...",
                AvatarId = "avatar_01",
                Position = GameClient.Dialogue.DialoguePosition.Left
            };

            var inputPort = GeneratePort(dialogueNode, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            dialogueNode.inputContainer.Add(inputPort);

            var nextPort = GeneratePort(dialogueNode, Direction.Output, Port.Capacity.Single);
            nextPort.portName = "Next";
            dialogueNode.outputContainer.Add(nextPort);

            var btnAddChoice = new Button(() => { AddChoicePort(dialogueNode); })
            {
                text = "Add Choice"
            };
            dialogueNode.titleButtonContainer.Add(btnAddChoice);

            var locales = LocalizationEditorSync.GetAvailableLocales();
            if (locales.Count == 0) locales.Add("default");

            foreach (var locale in locales)
            {
                string txtValue = "";
                if (existingTexts != null && existingTexts.TryGetValue(locale, out var t)) txtValue = t;
                dialogueNode.LocalizedTexts[locale] = txtValue;

                var txtContent = new TextField($"Text ({locale}):");
                txtContent.RegisterValueChangedCallback(evt => { dialogueNode.LocalizedTexts[locale] = evt.newValue; });
                txtContent.value = txtValue;
                dialogueNode.mainContainer.Add(txtContent);
            }

            var txtSpeaker = new TextField("Speaker:");
            txtSpeaker.RegisterValueChangedCallback(evt => { dialogueNode.SpeakerKey = evt.newValue; });
            txtSpeaker.value = dialogueNode.SpeakerKey;
            dialogueNode.mainContainer.Add(txtSpeaker);

            var txtAvatar = new TextField("Avatar ID:");
            txtAvatar.RegisterValueChangedCallback(evt => { dialogueNode.AvatarId = evt.newValue; });
            txtAvatar.value = dialogueNode.AvatarId;
            dialogueNode.mainContainer.Add(txtAvatar);

            var enumPos = new UnityEngine.UIElements.EnumField("Position", dialogueNode.Position);
            enumPos.RegisterValueChangedCallback(evt => { dialogueNode.Position = (GameClient.Dialogue.DialoguePosition)evt.newValue; });
            dialogueNode.mainContainer.Add(enumPos);

            dialogueNode.SetPosition(new Rect(position, new Vector2(250, 200)));
            
            AddElement(dialogueNode);
            return dialogueNode;
        }

        public void AddChoicePort(DialogueNodeView node, string overridingPortName = "")
        {
            var choicePort = GeneratePort(node, Direction.Output, Port.Capacity.Single);

            var outputPortCount = node.outputContainer.Query("connector").ToList().Count;
            choicePort.portName = !string.IsNullOrEmpty(overridingPortName) ? overridingPortName : $"Choice {outputPortCount}";
            
            var textField = new TextField
            {
                name = string.Empty,
                value = choicePort.portName
            };
            textField.RegisterValueChangedCallback(evt => choicePort.portName = evt.newValue);
            choicePort.contentContainer.Add(new Label("  "));
            choicePort.contentContainer.Add(textField);

            var deleteButton = new Button(() => RemovePort(node, choicePort))
            {
                text = "X"
            };
            choicePort.contentContainer.Add(deleteButton);

            node.outputContainer.Add(choicePort);
            node.RefreshPorts();
            node.RefreshExpandedState();
        }

        private void RemovePort(DialogueNodeView node, Port socket)
        {
            var targetEdge = socket.connections.GetEnumerator();
            while(targetEdge.MoveNext())
            {
                targetEdge.Current.input.Disconnect(targetEdge.Current);
                RemoveElement(targetEdge.Current);
            }
            node.outputContainer.Remove(socket);
            node.RefreshPorts();
            node.RefreshExpandedState();
        }

        private Port GeneratePort(Node node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
        {
            return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float)); 
        }
    }
}
