using UnityEngine;
using DG.Tweening;

namespace GameClient.Utils
{
    public class SimpleScroller : MonoBehaviour
    {
        [Header("Tốc độ cơ bản")]
        public Vector2 baseSpeed = new Vector2(0.1f, 0f);
        
        [Header("Độ ngẫu nhiên (0 = không random)")]
        [Range(0f, 2f)]
        public float speedVariation = 0.5f;

        [Header("Tự động quay lại khi đi quá xa")]
        public bool autoLoop = true;
        public float loopBoundary = 20f;
        public bool randomStartPos = true;

        private Vector2 _currentSpeed;
        private Vector3 _startPos;

        void Start()
        {
            _startPos = transform.localPosition;

            float multiplier = Random.Range(1.0f - speedVariation, 1.0f + speedVariation);
            _currentSpeed = baseSpeed * multiplier;

            if (randomStartPos)
            {
                float randomOffset = Random.Range(-loopBoundary, loopBoundary);
                transform.localPosition = _startPos + new Vector3(randomOffset, 0, 0);
            }

            StartScrolling();
        }

        private void StartScrolling()
        {
            if (baseSpeed.x == 0) return;

            float targetX = _currentSpeed.x > 0 ? _startPos.x + loopBoundary : _startPos.x - loopBoundary;
            float dist = Mathf.Abs(targetX - transform.localPosition.x);
            float time = dist / Mathf.Abs(_currentSpeed.x);

            transform.DOLocalMoveX(targetX, time).SetEase(Ease.Linear).OnComplete(() =>
            {
                if (autoLoop)
                {
                    float resetX = _currentSpeed.x > 0 ? _startPos.x - loopBoundary : _startPos.x + loopBoundary;
                    transform.localPosition = new Vector3(resetX, transform.localPosition.y, transform.localPosition.z);
                    
                    float multiplier = Random.Range(1.0f - speedVariation, 1.0f + speedVariation);
                    _currentSpeed = baseSpeed * multiplier;
                    
                    StartScrolling();
                }
            });
        }
    }
}
