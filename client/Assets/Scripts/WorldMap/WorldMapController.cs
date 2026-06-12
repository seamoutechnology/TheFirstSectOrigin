using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameClient.Core;
using GameClient.Managers;
using GameClient.Gameplay.World;

namespace GameClient.WorldMap
{
    public class WorldMapController : Singleton<WorldMapController>
    {
        protected override bool DontDestroy => false;

        [Header("PVE Stages Configuration")]
        [Tooltip("Danh sách các ải PVE xếp theo thứ tự")]
        public List<StageData> stages = new List<StageData>();

        [Header("UI Reference Settings")]
        [Tooltip("Nút bấm quay lại Tông Môn")]
        public Button btnBackToSect;
        
        [Tooltip("Container (ví dụ: Grid/Vertical/Horizontal Layout) chứa các nút chọn Ải")]
        public Transform stagesContainer;

        [Tooltip("Prefab nút chọn ải. Nếu để trống sẽ tự tạo Button mặc định.")]
        public GameObject stageButtonPrefab;

        [Tooltip("Scroll Rect chứa danh sách các ải để tự động cuộn")]
        public ScrollRect stagesScrollRect;

        private void Start()
        {
            Debug.Log("[WorldMapController] Khởi tạo Bản đồ Chọn Ải PVE...");

            // 1. Gán sự kiện nút quay lại Tông Môn
            if (btnBackToSect == null)
            {
                btnBackToSect = GameObject.Find("BtnBackToSect")?.GetComponent<Button>() 
                                ?? GameObject.Find("BackToSectButton")?.GetComponent<Button>();
            }

            if (btnBackToSect != null)
            {
                btnBackToSect.onClick.AddListener(OnBackToSectClicked);
            }
            else
            {
                Debug.LogWarning("[WorldMapController] Không tìm thấy Button quay lại Tông môn. Bạn hãy tạo một Button tên 'BtnBackToSect' trong Scene.");
            }

            // 2. Tự động tìm Container nếu chưa gán
            if (stagesContainer == null)
            {
                stagesContainer = GameObject.Find("StagesContainer")?.transform;
            }

            // 3. Khởi tạo danh sách các ải
            LoadAndRenderStages();
        }

        private void LoadAndRenderStages()
        {
            // Tải tự động từ Resources nếu danh sách trống làm mặc định
            if (stages == null || stages.Count == 0)
            {
                var loadedStages = Resources.LoadAll<StageData>("GameData/Stages");
                if (loadedStages != null && loadedStages.Length > 0)
                {
                    var filtered = loadedStages
                        .Where(s => s != null && s.stageId != "Fallback" && s.name != "Stage_Fallback")
                        .ToList();
                    stages.AddRange(filtered);
                }
            }

            // Hậu xử lý phòng thủ: Loại bỏ null/fallback, lọc trùng lặp stageId và sắp xếp thứ tự
            stages = stages
                .Where(s => s != null && s.stageId != "Fallback" && s.name != "Stage_Fallback")
                .GroupBy(s => s.stageId)
                .Select(g => g.First())
                .OrderBy(s => s.stageId)
                .ToList();

            if (stagesContainer == null)
            {
                Debug.LogWarning("[WorldMapController] Chưa cấu hình 'stagesContainer' trong Hierarchy để hiển thị danh sách các ải!");
                return;
            }

            // Xóa các nút cũ trong container trước khi vẽ
            foreach (Transform child in stagesContainer)
            {
                Destroy(child.gameObject);
            }

            // Sinh các nút chọn ải
            for (int i = 0; i < stages.Count; i++)
            {
                StageData stage = stages[i];
                if (stage == null) continue;

                // Kiểm tra xem ải đã được mở khóa chưa
                bool isUnlocked = true;
                if (!string.IsNullOrEmpty(stage.requiredStageId))
                {
                    string completed = PlayerPrefs.GetString("CompletedStages", "");
                    var completedList = completed.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToList();
                    isUnlocked = completedList.Contains(stage.requiredStageId);
                }

                GameObject btnObj;
                if (stageButtonPrefab != null)
                {
                    btnObj = Instantiate(stageButtonPrefab, stagesContainer);
                    var item = btnObj.GetComponent<StageButtonItem>();
                    if (item != null)
                    {
                        item.SetData(stage, i);
                    }
                }
                else
                {
                    // Tạo Button giả lập bằng Code nếu thiếu Prefab
                    btnObj = new GameObject($"StageButton_{stage.stageId}");
                    btnObj.transform.SetParent(stagesContainer, false);
                    btnObj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
                    btnObj.AddComponent<Button>();

                    var textObj = new GameObject("Text");
                    textObj.transform.SetParent(btnObj.transform, false);
                    var txt = textObj.AddComponent<TMPro.TextMeshProUGUI>();
                    txt.text = $"{i + 1}. {stage.stageId}";
                    txt.fontSize = 24;
                    txt.alignment = TMPro.TextAlignmentOptions.Center;
                    txt.color = Color.white;

                    var rect = textObj.GetComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.sizeDelta = Vector2.zero;
                }

                // Gán sự kiện click vào ải và điều chỉnh tương tác dựa trên trạng thái khóa
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = isUnlocked;
                    
                    // Nếu ải bị khóa, làm mờ/tối nút để người chơi nhận biết
                    if (!isUnlocked)
                    {
                        var canvasGroup = btnObj.GetComponent<CanvasGroup>();
                        if (canvasGroup == null)
                        {
                            canvasGroup = btnObj.AddComponent<CanvasGroup>();
                        }
                        canvasGroup.alpha = 0.5f; // Làm mờ 50%
                    }
                    
                    btn.onClick.AddListener(() => OnStageSelected(stage));
                }
            }

            // Tự động cuộn tới ải mới nhất sau khi sinh các nút
            if (stagesScrollRect != null)
            {
                StartCoroutine(Co_FocusOnLatestUnlockedStage());
            }
        }

        private IEnumerator Co_FocusOnLatestUnlockedStage()
        {
            // Chờ 1 frame để Unity UI Layout xây dựng lại kích thước Content
            yield return new WaitForEndOfFrame();

            if (stages == null || stages.Count == 0 || stagesScrollRect == null) yield break;

            int latestUnlockedIndex = 0;
            string completed = PlayerPrefs.GetString("CompletedStages", "");
            var completedList = completed.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToList();

            for (int i = 0; i < stages.Count; i++)
            {
                var stage = stages[i];
                if (stage == null) continue;

                bool isUnlocked = true;
                if (!string.IsNullOrEmpty(stage.requiredStageId))
                {
                    isUnlocked = completedList.Contains(stage.requiredStageId);
                }

                if (isUnlocked)
                {
                    latestUnlockedIndex = i;
                }
            }

            if (stages.Count > 1)
            {
                float t = (float)latestUnlockedIndex / (stages.Count - 1);
                // Với ScrollRect dọc: 1 là trên cùng (index 0), 0 là dưới cùng (index cuối)
                stagesScrollRect.verticalNormalizedPosition = 1f - t;
            }
            else
            {
                stagesScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void OnStageSelected(StageData stage)
        {
            Debug.Log($"[WorldMapController] Chọn ải: {stage.stageId} - Mở giao diện chuẩn bị chiến đấu...");
            if (UIManager.Instance != null)
            {
                // Mở bảng chuẩn bị chiến đấu (BattlePrepPanel) và truyền cấu hình ải vào
                UIManager.Instance.OpenPanel("UI_BattlePrepPanel", stage, false);
            }
        }

        private async void OnBackToSectClicked()
        {
            Debug.Log("[WorldMapController] Trở về Tông Môn...");
            if (MapManager.Instance != null)
            {
                await MapManager.Instance.LoadMapAsync(MapType.LocalBase);
            }
        }
    }
}
