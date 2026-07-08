using System;
using System.Collections.Generic;
using UnityEngine;
using GameClient.Core;
using GameClient.Dialogue;
using GameClient.UI;

namespace GameClient.Managers
{
    [Serializable]
    public class TutorialStep
    {
        public string StepID;
        public string Title;
        public string Description;
        public string DialogueSequenceID; // Nếu có, phát hội thoại trước khi bắt đầu nhiệm vụ này
        public string TargetUIPanel;      // Panel cần người chơi mở (e.g. BuildMenuPanel)
        public string TargetHighlightID;  // ID của thành phần UI cần highlight hướng dẫn chạm
        public string TriggerActionType;  // Hành động người chơi cần thực hiện để hoàn thành (e.g. BuildMainHall)
        public bool IsCompleted;
    }

    public class TutorialManager : Singleton<TutorialManager>
    {
        private const string PREFS_KEY_STEP = "Tutorial_CurrentStepIndex";
        private const string PREFS_KEY_COMPLETED = "Tutorial_AllCompleted";

        [Header("Tutorial Config")]
        public List<TutorialStep> Steps = new List<TutorialStep>();

        public int CurrentStepIndex { get; private set; } = 0;
        public bool IsTutorialCompleted { get; private set; } = false;

        public event Action<TutorialStep> OnStepStarted;
        public event Action<TutorialStep> OnStepCompleted;
        public event Action OnTutorialFinished;

        private readonly Dictionary<string, TutorialTarget> _registeredTargets = new Dictionary<string, TutorialTarget>();
        private TutorialGuideOverlay _currentOverlay;

        protected override void Awake()
        {
            base.Awake();
            LoadProgress();
            InitializeDefaultSteps();
        }

        private void InitializeDefaultSteps()
        {
            if (Steps.Count == 0)
            {
                Steps.Add(new TutorialStep
                {
                    StepID = "welcome_dialogue",
                    Title = "Nhập môn",
                    Description = "Lắng nghe lời dẫn nhập từ Chưởng Môn",
                    DialogueSequenceID = "intro_cutscene",
                    TriggerActionType = "ListenDialogue"
                });

                Steps.Add(new TutorialStep
                {
                    StepID = "open_build_menu",
                    Title = "Khai khẩn",
                    Description = "Mở bảng Xây dựng để xem danh sách công trình",
                    TargetHighlightID = "BuildMenuButton", // Highlight nút mở Build Menu trên HUD
                    TriggerActionType = "OpenBuildMenu"
                });

                Steps.Add(new TutorialStep
                {
                    StepID = "build_main_hall",
                    Title = "Xây dựng Chính Điện",
                    Description = "Tiến hành đặt Chính Điện lên mảnh đất của Tông Môn",
                    TriggerActionType = "Build_main_hall"
                });
            }
        }

        public void RegisterTarget(string targetID, TutorialTarget target)
        {
            _registeredTargets[targetID] = target;
            Debug.Log($"[TutorialManager] Đã đăng ký UI Target: {targetID}");

            // Nếu đang chờ ở step hiện tại có yêu cầu highlight ID này, tiến hành spawn overlay ngay lập tức
            if (!IsTutorialCompleted && CurrentStepIndex < Steps.Count)
            {
                var step = Steps[CurrentStepIndex];
                if (step.TargetHighlightID == targetID && _currentOverlay == null)
                {
                    SpawnGuideOverlay(target, step.TriggerActionType);
                }
            }
        }

        public void UnregisterTarget(string targetID)
        {
            if (_registeredTargets.ContainsKey(targetID))
            {
                _registeredTargets.Remove(targetID);
                Debug.Log($"[TutorialManager] Huỷ đăng ký UI Target: {targetID}");
            }
        }

        public void StartTutorial()
        {
            if (IsTutorialCompleted)
            {
                Debug.Log("[TutorialManager] Tutorial đã hoàn thành từ trước.");
                return;
            }

            ExecuteStep(CurrentStepIndex);
        }

        private void ExecuteStep(int index)
        {
            // Dọn dẹp overlay cũ nếu có
            ClearCurrentOverlay();

            if (index < 0 || index >= Steps.Count)
            {
                FinishTutorial();
                return;
            }

            CurrentStepIndex = index;
            var step = Steps[index];
            Debug.Log($"[TutorialManager] Bắt đầu Step {index}: {step.Title} ({step.StepID})");

            OnStepStarted?.Invoke(step);

            // 1. Phát hội thoại nếu có
            if (!string.IsNullOrEmpty(step.DialogueSequenceID))
            {
                if (DialogueManager.Instance != null)
                {
                    Debug.Log($"[TutorialManager] Phát hội thoại: {step.DialogueSequenceID}");
                    DialogueManager.Instance.PlaySequence(step.DialogueSequenceID);
                    DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
                }
            }

            // 2. Tự động mở panel nếu có
            if (!string.IsNullOrEmpty(step.TargetUIPanel))
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.OpenPanel(step.TargetUIPanel);
                }
            }

            // 3. Hiển thị mũi tên hướng dẫn chỉ vào nút nếu phần tử đó đã đăng ký sẵn
            if (!string.IsNullOrEmpty(step.TargetHighlightID))
            {
                if (_registeredTargets.TryGetValue(step.TargetHighlightID, out TutorialTarget target))
                {
                    SpawnGuideOverlay(target, step.TriggerActionType);
                }
                else
                {
                    Debug.Log($"[TutorialManager] Step hiện tại yêu cầu highlight '{step.TargetHighlightID}' nhưng phần tử chưa xuất hiện/chưa đăng ký.");
                }
            }
        }

        private void SpawnGuideOverlay(TutorialTarget target, string actionToTrigger)
        {
            ClearCurrentOverlay();

            // Tìm canvas chính để gắn overlay lên trên cùng
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject overlayGo = new GameObject("TutorialGuideOverlay");
            overlayGo.transform.SetParent(canvas.transform, false);

            _currentOverlay = overlayGo.AddComponent<TutorialGuideOverlay>();
            _currentOverlay.Setup(target, actionToTrigger);
        }

        private void ClearCurrentOverlay()
        {
            if (_currentOverlay != null)
            {
                Destroy(_currentOverlay.gameObject);
                _currentOverlay = null;
            }
        }

        private void HandleDialogueEnded()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
            }

            var step = Steps[CurrentStepIndex];
            if (step.TriggerActionType == "ListenDialogue")
            {
                CompleteCurrentStep();
            }
        }

        public void TriggerAction(string actionType)
        {
            if (IsTutorialCompleted) return;

            if (CurrentStepIndex >= 0 && CurrentStepIndex < Steps.Count)
            {
                var step = Steps[CurrentStepIndex];
                if (step.TriggerActionType.Equals(actionType, StringComparison.OrdinalIgnoreCase))
                {
                    CompleteCurrentStep();
                }
            }
        }

        public void CompleteCurrentStep()
        {
            if (CurrentStepIndex < 0 || CurrentStepIndex >= Steps.Count) return;

            var step = Steps[CurrentStepIndex];
            step.IsCompleted = true;
            Debug.Log($"[TutorialManager] Hoàn thành Step {CurrentStepIndex}: {step.Title}");

            OnStepCompleted?.Invoke(step);

            CurrentStepIndex++;
            SaveProgress();

            ExecuteStep(CurrentStepIndex);
        }

        public void SkipTutorial()
        {
            Debug.Log("[TutorialManager] Skip Tutorial.");
            FinishTutorial();
        }

        private void FinishTutorial()
        {
            ClearCurrentOverlay();
            IsTutorialCompleted = true;
            SaveProgress();
            Debug.Log("[TutorialManager] Đã hoàn tất toàn bộ Tutorial!");
            OnTutorialFinished?.Invoke();
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt(PREFS_KEY_STEP, CurrentStepIndex);
            PlayerPrefs.SetInt(PREFS_KEY_COMPLETED, IsTutorialCompleted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            CurrentStepIndex = PlayerPrefs.GetInt(PREFS_KEY_STEP, 0);
            IsTutorialCompleted = PlayerPrefs.GetInt(PREFS_KEY_COMPLETED, 0) == 1;
        }

        public void ResetTutorial()
        {
            ClearCurrentOverlay();
            PlayerPrefs.DeleteKey(PREFS_KEY_STEP);
            PlayerPrefs.DeleteKey(PREFS_KEY_COMPLETED);
            PlayerPrefs.Save();
            
            CurrentStepIndex = 0;
            IsTutorialCompleted = false;
            foreach (var step in Steps)
            {
                step.IsCompleted = false;
            }

            Debug.Log("[TutorialManager] Đã Reset tiến trình Tutorial.");
        }
    }
}
