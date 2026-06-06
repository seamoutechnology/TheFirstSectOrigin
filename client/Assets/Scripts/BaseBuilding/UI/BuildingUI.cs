using UnityEngine;
using UnityEngine.UI;
using GameClient.BaseBuilding.Core;

namespace GameClient.BaseBuilding.UI
{
    public class BuildingUI : MonoBehaviour
    {
        public BuildingController buildingController;

        [Header("UI Buttons")]
        public Button btnConfirm;
        public Button btnRotate;
        public Button btnCancel;

        private void Start()
        {
            if (buildingController == null)
            {
                buildingController = FindFirstObjectByType<BuildingController>();
            }

            if (btnConfirm != null) btnConfirm.onClick.AddListener(() => buildingController.TryPlaceBuilding());
            if (btnRotate != null) btnRotate.onClick.AddListener(() => buildingController.RotateBuilding());
            if (btnCancel != null) btnCancel.onClick.AddListener(() => buildingController.CancelPlacement());
            
            HideUI();
        }

        public void ShowUI()
        {
            gameObject.SetActive(true);
        }

        public void HideUI()
        {
            gameObject.SetActive(false);
        }

    }
}
