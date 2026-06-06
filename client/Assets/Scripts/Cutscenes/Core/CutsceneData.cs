using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameClient.Cutscenes.Core
{
    [Serializable]
    public class CutsceneGraphData
    {
        public string cutsceneId;
        public List<CutsceneEntityData> entities = new List<CutsceneEntityData>();
        public List<CutsceneNodeLinkData> nodeLinks = new List<CutsceneNodeLinkData>();
        public List<CutsceneNodeData> nodes = new List<CutsceneNodeData>();
    }

    [Serializable]
    public class CutsceneNodeLinkData
    {
        public string baseNodeGuid;
        public string portName;
        public string targetNodeGuid;
    }

    [Serializable]
    public class CutsceneEntityData
    {
        public string id;
        public string prefabPath; // "Primitive:Cube", "Primitive:Sphere", "Primitive:Capsule"
        public Vector3 startPos;
    }

    public enum CutsceneNodeType
    {
        Entry,
        MoveTo,
        Wait,
        Unparent,
        Dialogue,
        OpenUI,
        CameraMove,
        CameraShake,
        PlaySound,
        ParentTo,
        PlayAnimation,
        FindBuilding,
        DestroyEntity
    }

    [Serializable]
    public class CutsceneNodeData
    {
        public string guid;
        public CutsceneNodeType type;
        public Vector2 position; // Position in Editor Graph
        
        public string targetEntityId;
        public Vector3 targetPos;
        public float duration;
        public string easeType; // Custom string for Ease, e.g., "Linear", "OutQuad"
        
        public string dialogueTable;
        public string dialogueKey;
        
        public string panelName;
        public bool isLoadByPlatform = true;
        
        public bool isCameraMoveToEntity;
        public float cameraZoom;
        public float shakeStrength;
        public int shakeVibrato;
        
        public string audioTable;
        public string audioKey;
        
        public string parentEntityId;
        
        public string animationName;
    }
}
