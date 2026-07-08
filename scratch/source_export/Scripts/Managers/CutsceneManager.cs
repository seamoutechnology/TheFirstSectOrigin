using System.Collections.Generic;
using UnityEngine;
using GameClient.Core;
using GameClient.Cutscenes.Core;
using System.Linq;

namespace GameClient.Managers
{
    public class CutsceneManager : Singleton<CutsceneManager>
    {
        [SerializeField] private List<BaseCutscene> _registeredCutscenes = new List<BaseCutscene>();
        
        private BaseCutscene _currentCutscene;

        public void RegisterCutscene(BaseCutscene cutscene)
        {
            if (!_registeredCutscenes.Contains(cutscene))
            {
                _registeredCutscenes.Add(cutscene);
            }
        }

        public void PlayCutscene(string id)
        {
            if (_currentCutscene != null && _currentCutscene.IsPlaying)
            {
                Debug.LogWarning($"[CutsceneManager] Không thể phát {id} vì {_currentCutscene.CutsceneId} đang chiếu!");
                return;
            }

            var cutscene = _registeredCutscenes.FirstOrDefault(c => c.CutsceneId == id);
            if (cutscene != null)
            {
                _currentCutscene = cutscene;
                cutscene.gameObject.SetActive(true);
                cutscene.Play();
            }
            else
            {
                Debug.LogError($"[CutsceneManager] Không tìm thấy Cutscene với ID: {id}");
            }
        }

        public void SkipCurrentCutscene()
        {
            if (_currentCutscene != null && _currentCutscene.IsPlaying)
            {
                _currentCutscene.Skip();
            }
        }
    }
}
