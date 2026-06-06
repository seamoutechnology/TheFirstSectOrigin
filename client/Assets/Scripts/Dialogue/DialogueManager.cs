using System;
using System.Collections.Generic;
using UnityEngine;
using GameClient.Core;
using GameClient.Managers; // Lấy LocalizationManager

namespace GameClient.Dialogue
{
    public class DialogueManager : Singleton<DialogueManager>
    {
        private const string JSON_PATH = "Data/DialogueDatabase"; // Load từ Resources

        private Dictionary<string, DialogueSequence> _sequencesMap = new Dictionary<string, DialogueSequence>();

        private Stack<string> _sequenceStack = new Stack<string>();
        public DialogueSequence CurrentSequence { get; private set; }
        public DialogueNode CurrentNode { get; private set; }

        public event Action<DialogueNode> OnNodeStarted;
        public event Action OnDialogueEnded;

        public bool IsAutoMode { get; set; } = false;
        public bool IsSkipMode { get; set; } = false;
        public float SpeedMultiplier { get; set; } = 1f;

        protected override void Awake()
        {
            base.Awake();
            LoadDatabaseFromLocal();
        }

        public void LoadDatabaseFromLocal()
        {
            TextAsset textAsset = GameClient.Core.ResourceManager.Instance.LoadFromResources<TextAsset>(JSON_PATH);
            if (textAsset != null)
            {
                ParseJsonDatabase(textAsset.text);
            }
        }

        public void ParseJsonDatabase(string json)
        {
            try
            {
                var db = JsonUtility.FromJson<DialogueDatabase>(json);
                if (db != null)
                {
                    _sequencesMap.Clear();
                    foreach (var seq in db.sequences)
                    {
                        _sequencesMap[seq.sequenceId] = seq;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DialogueManager] Lỗi Parse JSON: {ex.Message}");
            }
        }

        public void PlaySequence(string sequenceId)
        {
            if (_sequencesMap.ContainsKey(sequenceId))
            {
                _sequenceStack.Push(sequenceId);
                if (CurrentSequence == null)
                {
                    StartNextSequenceInStack();
                }
            }
            else
            {
                Debug.LogError($"[DialogueManager] Không tìm thấy Sequence: {sequenceId}");
            }
        }

        private void StartNextSequenceInStack()
        {
            if (_sequenceStack.Count > 0)
            {
                string nextSeqId = _sequenceStack.Pop();
                CurrentSequence = _sequencesMap[nextSeqId];
                PlayNode(CurrentSequence.entryNodeId);
            }
            else
            {
                CurrentSequence = null;
                CurrentNode = null;
                OnDialogueEnded?.Invoke();
            }
        }

        public void PlayNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                StartNextSequenceInStack(); // Chuyển sang Sequence tiếp theo trong Stack nếu có
                return;
            }

            CurrentNode = CurrentSequence.nodes.Find(n => n.nodeId == nodeId);
            if (CurrentNode != null)
            {
                OnNodeStarted?.Invoke(CurrentNode);
            }
            else
            {
                Debug.LogError($"[DialogueManager] Không tìm thấy Node: {nodeId}");
                StartNextSequenceInStack();
            }
        }

        public void NextNode()
        {
            if (CurrentNode != null && CurrentNode.choices.Count == 0)
            {
                PlayNode(CurrentNode.nextNodeId);
            }
        }

        public string GetLocalizedText(string key)
        {
            if (LocalizationManager.Instance != null)
            {
                return LocalizationManager.Instance.GetText(key);
            }
            return key; // Fallback
        }
    }
}
