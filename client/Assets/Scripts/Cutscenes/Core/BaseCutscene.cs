using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using GameClient.Managers;

namespace GameClient.Cutscenes.Core
{
    public abstract class BaseCutscene : MonoBehaviour, ICutscene
    {
        [SerializeField] private string cutsceneId;
        public string CutsceneId => cutsceneId;

        public bool IsPlaying { get; protected set; }

        protected int _activeNodesCount = 0;
        protected Sequence _currentSequence;

        public virtual void Play()
        {
            if (IsPlaying) return;
            IsPlaying = true;
            _activeNodesCount = 0;
            
            Debug.Log($"[Cutscene] Bắt đầu chiếu Cutscene: {CutsceneId}");
            EventManager.Instance.Emit(GameEvents.ON_CUTSCENE_STARTED, CutsceneId);
            
            StartExecution();
        }

        protected abstract void StartExecution();

        protected void CompleteNode()
        {
            if (!IsPlaying) return;
            
            _activeNodesCount--;
            EventManager.Instance.Emit(GameEvents.ON_CUTSCENE_STEP_COMPLETED, CutsceneId);
            
            if (_activeNodesCount <= 0)
            {
                Stop();
            }
        }

        public virtual void Pause()
        {
            if (!IsPlaying) return;
            _currentSequence?.Pause();
        }

        public virtual void Resume()
        {
            if (!IsPlaying) return;
            _currentSequence?.Play();
        }

        public virtual void Stop()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            
            _currentSequence?.Kill();
            
            Debug.Log($"[Cutscene] Kết thúc Cutscene: {CutsceneId}");
            EventManager.Instance.Emit(GameEvents.ON_CUTSCENE_FINISHED, CutsceneId);
            
            gameObject.SetActive(false);
        }

        public virtual void Skip()
        {
            Debug.Log($"[Cutscene] Bỏ qua Cutscene: {CutsceneId}");
            Stop();
        }
    }
}
