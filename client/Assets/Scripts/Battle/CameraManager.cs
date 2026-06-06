using UnityEngine;
using DG.Tweening;

namespace GameClient.Battle
{
    public class CameraManager : Singleton<CameraManager>
    {
        [Header("Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float defaultSize = 5f;
        [SerializeField] private Vector3 defaultPosition = new Vector3(0, 0, -10);

        protected override void Awake()
        {
            base.Awake();
            if (mainCamera == null) mainCamera = Camera.main;
            defaultSize = mainCamera.orthographicSize;
            defaultPosition = mainCamera.transform.position;
        }

        public void FocusOn(Vector3 targetPos, float zoomSize, float duration)
        {
            mainCamera.transform.DOKill();
            mainCamera.DOKill();
            
            targetPos.z = mainCamera.transform.position.z;
            mainCamera.transform.DOMove(targetPos, duration).SetEase(Ease.InOutSine);
            mainCamera.DOOrthoSize(zoomSize, duration).SetEase(Ease.InOutSine);
        }

        public void ResetCamera(float duration)
        {
            mainCamera.transform.DOKill();
            mainCamera.DOKill();
            
            mainCamera.transform.DOMove(defaultPosition, duration).SetEase(Ease.InOutSine);
            mainCamera.DOOrthoSize(defaultSize, duration).SetEase(Ease.InOutSine);
        }

        public void Shake(float duration, float magnitude)
        {
            mainCamera.transform.DOComplete(); // Hoàn thành các tween trước đó nếu có
            mainCamera.transform.DOShakePosition(duration, new Vector3(magnitude, magnitude, 0));
        }
    }
}
