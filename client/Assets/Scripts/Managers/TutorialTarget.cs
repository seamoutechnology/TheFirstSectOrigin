using UnityEngine;

namespace GameClient.Managers
{
    public class TutorialTarget : MonoBehaviour
    {
        [Tooltip("ID duy nhất để TutorialManager tìm và chỉ dẫn đến nút này")]
        public string TargetID;

        private void Start()
        {
            if (string.IsNullOrEmpty(TargetID))
            {
                Debug.LogWarning($"[TutorialTarget] TargetID trên '{gameObject.name}' trống!");
                return;
            }

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.RegisterTarget(TargetID, this);
            }
        }

        private void OnDestroy()
        {
            if (TutorialManager.Instance != null && !string.IsNullOrEmpty(TargetID))
            {
                TutorialManager.Instance.UnregisterTarget(TargetID);
            }
        }
    }
}
