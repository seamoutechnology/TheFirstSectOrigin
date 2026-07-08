#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using GameClient.Cutscenes.Core;
using System.Linq;

namespace GameClient.Cutscenes.Editor
{
    public class CutsceneGraphView : GraphView
    {
        public readonly Vector2 DefaultNodeSize = new Vector2(200, 250);

        public CutsceneGraphView()
        {
            var styleSheet = Resources.Load<StyleSheet>("CutsceneGraphView");
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            AddElement(GenerateEntryPointNode());
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            ports.ForEach(funcCall: port =>
            {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });
            return compatiblePorts;
        }

        private Port GeneratePort(CutsceneNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Multi)
        {
            return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
        }

        private CutsceneNode GenerateEntryPointNode()
        {
            var node = new CutsceneNode
            {
                title = "START",
                GUID = Guid.NewGuid().ToString(),
                NodeType = CutsceneNodeType.Entry,
                EntryPoint = true
            };

            var port = GeneratePort(node, Direction.Output);
            port.portName = "Next";
            node.outputContainer.Add(port);

            node.RefreshExpandedState();
            node.RefreshPorts();
            node.SetPosition(new Rect(100, 200, 100, 150));

            node.capabilities &= ~Capabilities.Deletable;
            node.capabilities &= ~Capabilities.Movable;

            return node;
        }

        public void CreateNode(string nodeName, CutsceneNodeType type)
        {
            var node = new CutsceneNode
            {
                title = nodeName,
                NodeType = type,
                GUID = Guid.NewGuid().ToString()
            };

            var inputPort = GeneratePort(node, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            node.inputContainer.Add(inputPort);

            var outputPort = GeneratePort(node, Direction.Output);
            outputPort.portName = "Next";
            node.outputContainer.Add(outputPort);

            node.AddCustomFields();
            
            node.RefreshExpandedState();
            node.RefreshPorts();
            node.SetPosition(new Rect(Vector2.zero, DefaultNodeSize));

            AddElement(node);
        }
    }
}
#endif
