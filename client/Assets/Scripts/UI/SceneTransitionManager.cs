using UnityEngine;
using System.Collections;
using GameClient.Core;
using DG.Tweening;

namespace GameClient.UI
{
    public class SceneTransitionManager : Singleton<SceneTransitionManager>
    {
        [Header("4 Đám mây ở 4 góc")]
        public Transform cloudTL;
        public Transform cloudTR;
        public Transform cloudBL;
        public Transform cloudBR;

        [Header("Cấu hình")]
        public float transitionDuration = GameConstants.UI.DEFAULT_TRANSITION_TIME;
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Vector3 _targetPos = Vector3.zero; // Tâm màn hình
        private Vector3[] _startPositions = new Vector3[4];

        protected override void Awake()
        {
            base.Awake();
            SaveStartPositions();
        }

        private void SaveStartPositions()
        {
            if (cloudTL) _startPositions[0] = cloudTL.localPosition;
            if (cloudTR) _startPositions[1] = cloudTR.localPosition;
            if (cloudBL) _startPositions[2] = cloudBL.localPosition;
            if (cloudBR) _startPositions[3] = cloudBR.localPosition;

            if (cloudTL) cloudTL.gameObject.SetActive(false);
            if (cloudTR) cloudTR.gameObject.SetActive(false);
            if (cloudBL) cloudBL.gameObject.SetActive(false);
            if (cloudBR) cloudBR.gameObject.SetActive(false);
        }

        public IEnumerator Co_EnterTransition()
        {
            Sequence seq = DOTween.Sequence();
            if (cloudTL) seq.Insert(0, cloudTL.DOLocalMove(_targetPos, transitionDuration).SetEase(transitionCurve));
            if (cloudTR) seq.Insert(0, cloudTR.DOLocalMove(_targetPos, transitionDuration).SetEase(transitionCurve));
            if (cloudBL) seq.Insert(0, cloudBL.DOLocalMove(_targetPos, transitionDuration).SetEase(transitionCurve));
            if (cloudBR) seq.Insert(0, cloudBR.DOLocalMove(_targetPos, transitionDuration).SetEase(transitionCurve));
            
            yield return seq.WaitForCompletion();
        }

        public IEnumerator Co_ExitTransition()
        {
            Sequence seq = DOTween.Sequence();
            if (cloudTL) seq.Insert(0, cloudTL.DOLocalMove(_startPositions[0], transitionDuration).SetEase(transitionCurve));
            if (cloudTR) seq.Insert(0, cloudTR.DOLocalMove(_startPositions[1], transitionDuration).SetEase(transitionCurve));
            if (cloudBL) seq.Insert(0, cloudBL.DOLocalMove(_startPositions[2], transitionDuration).SetEase(transitionCurve));
            if (cloudBR) seq.Insert(0, cloudBR.DOLocalMove(_startPositions[3], transitionDuration).SetEase(transitionCurve));
            
            yield return seq.WaitForCompletion();
        }

        public async System.Threading.Tasks.Task EnterTransitionAsync()
        {
            if (cloudTL) cloudTL.gameObject.SetActive(true);
            if (cloudTR) cloudTR.gameObject.SetActive(true);
            if (cloudBL) cloudBL.gameObject.SetActive(true);
            if (cloudBR) cloudBR.gameObject.SetActive(true);

            Sequence seq = DOTween.Sequence();
            if (cloudTL) seq.Insert(0, cloudTL.DOLocalMove(_targetPos, transitionDuration).SetEase(transitionCurve));
            if (cloudTR) seq.Insert(0, cloudTR.DOLocalMove(_targetPos, transitionDuration).SetEase(transitionCurve));
            if (cloudBL) seq.Insert(0, cloudBL.DOLocalMove(_targetPos, transitionDuration).SetEase(transitionCurve));
            if (cloudBR) seq.Insert(0, cloudBR.DOLocalMove(_targetPos, transitionDuration).SetEase(transitionCurve));
            
            await seq.AsyncWaitForCompletion();
        }

        public async System.Threading.Tasks.Task ExitTransitionAsync()
        {
            Sequence seq = DOTween.Sequence();
            if (cloudTL) seq.Insert(0, cloudTL.DOLocalMove(_startPositions[0], transitionDuration).SetEase(transitionCurve));
            if (cloudTR) seq.Insert(0, cloudTR.DOLocalMove(_startPositions[1], transitionDuration).SetEase(transitionCurve));
            if (cloudBL) seq.Insert(0, cloudBL.DOLocalMove(_startPositions[2], transitionDuration).SetEase(transitionCurve));
            if (cloudBR) seq.Insert(0, cloudBR.DOLocalMove(_startPositions[3], transitionDuration).SetEase(transitionCurve));
            
            await seq.AsyncWaitForCompletion();
            
            if (cloudTL) cloudTL.gameObject.SetActive(false);
            if (cloudTR) cloudTR.gameObject.SetActive(false);
            if (cloudBL) cloudBL.gameObject.SetActive(false);
            if (cloudBR) cloudBR.gameObject.SetActive(false);
        }

        public void TransitionToScene(string sceneName)
        {
            StartCoroutine(Co_TransitionToScene(sceneName));
        }

        private IEnumerator Co_TransitionToScene(string sceneName)
        {
            yield return StartCoroutine(Co_EnterTransition());

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClearAllPanels();
            }

            var asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            yield return StartCoroutine(Co_ExitTransition());
        }
    }
}
