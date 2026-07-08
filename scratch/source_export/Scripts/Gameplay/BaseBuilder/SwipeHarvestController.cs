using UnityEngine;
using TFSO.Managers;

namespace GameClient.Gameplay.BaseBuilder
{
    public class SwipeHarvestController : MonoBehaviour
    {
        [Header("Settings")]
        public LayerMask buildingLayer;
        
        private Camera _cam;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
        }

        private void Update()
        {
            if (SettingsManager.Instance != null && !SettingsManager.Instance.CurrentSettings.EnableSwipeHarvest) return;

            bool isDragging = false;
            Vector2 screenPos = Vector2.zero;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Began)
                {
                    isDragging = true;
                    screenPos = touch.position;
                }
            }
            else if (Input.GetMouseButton(0)) // Hỗ trợ cả Editor và PC
            {
                isDragging = true;
                screenPos = Input.mousePosition;
            }

            if (isDragging)
            {
                ProcessSwipe(screenPos);
            }
            
            if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
            {
            }
        }

        private void ProcessSwipe(Vector2 screenPosition)
        {
            Ray ray = _cam.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000f, buildingLayer))
            {
                BuildingInstance building = hit.collider.GetComponent<BuildingInstance>();
                if (building != null && building.HasResourcesToHarvest())
                {
                    building.CollectResourcesVisually();
                    SpawnFloatingCoins(hit.point);

                    HarvestSyncManager.Instance.QueueHarvest(building.Data.BuildingID);
                }
            }
        }

        private void SpawnFloatingCoins(Vector3 position)
        {
            Debug.Log("[SwipeHarvest] *Ting Ting* Hiệu ứng tiền bay lên!");
        }
    }
}
