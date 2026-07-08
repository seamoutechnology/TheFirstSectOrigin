using UnityEngine;
using UnityEngine.EventSystems;
using GameClient;

namespace GameClient.UI
{
    public class ItemPreviewTrigger : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Điền Mã ID của vật phẩm cần xem trước tại ô này")]
        public string itemCode;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(itemCode)) return;

            // Mở UI_ItemTooltipPanel qua UIManager và truyền ID vật phẩm vào làm tham số
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel("UI_ItemTooltipPanel", itemCode, false);
            }
        }
    }
}
