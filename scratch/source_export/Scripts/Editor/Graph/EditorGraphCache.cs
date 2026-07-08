using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameClient.EditorTools.Graph
{
    [Serializable]
    public class EditorNodePosition
    {
        public string nodeId;
        public Vector2 position;
    }

    [Serializable]
    public class EditorGraphCache
    {
        public List<EditorNodePosition> nodePositions = new List<EditorNodePosition>();
    }
}
