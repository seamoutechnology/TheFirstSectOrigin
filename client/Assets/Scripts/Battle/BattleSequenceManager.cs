using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameClient.Core;

namespace GameClient.Battle
{
    public class BattleSequenceManager : Singleton<BattleSequenceManager>
    {
        private Queue<BattleCommand> _commandQueue = new Queue<BattleCommand>();
        private bool _isPlaying = false;

        [SerializeField] private UI.Layout.UILayoutBuilder layoutBuilder; // Reference tới UI Builder
        [SerializeField] private GameObject cinematicOverlay; // Panel đen che sân đấu

        public GameObject GetUnitGameObject(string unitId)
        {
            return layoutBuilder != null ? layoutBuilder.GetElement(unitId) : null;
        }

        public void AddCommand(BattleCommand cmd)
        {
            _commandQueue.Enqueue(cmd);
            if (!_isPlaying) StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            _isPlaying = true;
            while (_commandQueue.Count > 0)
            {
                BattleCommand cmd = _commandQueue.Dequeue();
                yield return cmd.Execute();
            }
            _isPlaying = false;
        }

        public IEnumerator PlayCinematic(string skillId)
        {
            Debug.Log($"[Cinematic] Khởi động hiệu ứng đặc biệt cho {skillId}");
            
            if (cinematicOverlay != null) cinematicOverlay.SetActive(true);
            
            yield return new WaitForSeconds(3.0f); // Giả lập thời gian hiệu ứng

            if (cinematicOverlay != null) cinematicOverlay.SetActive(false);
        }
    }
}
