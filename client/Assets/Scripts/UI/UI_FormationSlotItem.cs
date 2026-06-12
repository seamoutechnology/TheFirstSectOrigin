using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameClient.UI
{
    public class UI_FormationSlotItem : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image imgHero;      // Ảnh hiển thị tướng đứng/spawn
        [SerializeField] private Image imgTextBg;    // Ảnh nền chữ nằm đè
        [SerializeField] private TMP_Text txtStatus; // Text hiển thị trạng thái/tên tướng

        public Button Button { get; private set; }

        private void Awake()
        {
            Button = GetComponent<Button>();
            if (Button == null) Button = gameObject.AddComponent<Button>();
            
            // Tự động tìm kiếm các component con nếu chưa được gán trong Inspector
            if (imgHero == null)
            {
                var heroTrans = transform.Find("Hero");
                if (heroTrans != null) imgHero = heroTrans.GetComponent<Image>();
            }
            
            if (imgTextBg == null)
            {
                var bgTrans = transform.Find("Image");
                if (bgTrans != null) imgTextBg = bgTrans.GetComponent<Image>();
            }
            
            if (txtStatus == null)
            {
                txtStatus = GetComponentInChildren<TMP_Text>();
            }

            if (imgHero != null) imgHero.enabled = false;
        }

        public void SetStatusText(string text)
        {
            if (txtStatus != null) txtStatus.text = text;
        }

        public void SetHeroVisual(Sprite sprite, bool show)
        {
            if (imgHero != null)
            {
                imgHero.sprite = sprite;
                imgHero.enabled = show && sprite != null;
            }
        }

        public void SetTextBgActive(bool active)
        {
            if (imgTextBg != null)
            {
                imgTextBg.gameObject.SetActive(active);
            }
        }
    }
}
