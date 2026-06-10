using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameClient.UI
{
    public class BuildItem : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Ảnh hiển thị công trình")]
        public Image imgPreview;
        
        [Tooltip("Text hiển thị tên công trình")]
        public TMP_Text txtName;
        
        [Tooltip("Khung nền đen bao quanh text giới hạn (để ẩn đi khi không giới hạn)")]
        public GameObject limitBg;
        
        [Tooltip("Text hiển thị số lượng giới hạn (ví dụ: 5/5)")]
        public TMP_Text txtLimit;
        
        [Tooltip("Button hành động để click xây dựng")]
        public Button btnAction;
        
        [Tooltip("Text hiển thị trạng thái nút (ví dụ: Xây dựng, Đã xây xong)")]
        public TMP_Text txtBtnStatus;
    }
}
