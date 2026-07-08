using System;
using System.Collections.Generic;

namespace GameClient.Dialogue
{
    public enum DialoguePosition
    {
        Left,
        Right,
        Center
    }

    [Serializable]
    public class DialogueChoice
    {
        public string textKey;
        public string nextNodeId; // Node ID nhảy tới khi chọn nhánh này
    }

    [Serializable]
    public class DialogueNode
    {
        public string nodeId; // ID duy nhất của Node này trong lưới
        public string textKey;
        public string speakerNameKey;
        public string avatarId;
        public DialoguePosition position;
        
        public string nextNodeId;
        
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }

    [Serializable]
    public class DialogueSequence
    {
        public string sequenceId; // Ví dụ: "tutorial_intro"
        public string entryNodeId; // Node bắt đầu của chuỗi này
        public List<DialogueNode> nodes = new List<DialogueNode>();
    }

    [Serializable]
    public class DialogueDatabase
    {
        public List<DialogueSequence> sequences = new List<DialogueSequence>();
    }
}
