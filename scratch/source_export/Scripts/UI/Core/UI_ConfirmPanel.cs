using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using GameClient.UI.Core;

namespace GameClient.UI
{
    public class UI_ConfirmPanel : BaseUIPanel
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text txtTitle;
        [SerializeField] private TMP_Text txtContent;
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private Transform buttonContainer;

        public struct DialogButtonInfo
        {
            public string text;
            public Action callback;
            public bool isPrimary;
        }

        public void SetupDialog(
            string title, 
            string content, 
            string acceptText, 
            string denyText, 
            Action onAccept, 
            Action onDeny = null)
        {
            var buttons = new System.Collections.Generic.List<DialogButtonInfo>();
            if (!string.IsNullOrEmpty(denyText))
            {
                buttons.Add(new DialogButtonInfo { text = denyText, callback = onDeny, isPrimary = false });
            }
            if (!string.IsNullOrEmpty(acceptText))
            {
                buttons.Add(new DialogButtonInfo { text = acceptText, callback = onAccept, isPrimary = true });
            }

            SetupDialog(title, content, buttons);
        }

        public void SetupDialog(
            string title, 
            string content, 
            System.Collections.Generic.List<DialogButtonInfo> buttons)
        {
            if (txtTitle != null) txtTitle.text = title;
            if (txtContent != null) txtContent.text = content;

            var container = buttonContainer != null ? buttonContainer : this.transform;

            // Xóa các nút cũ trong container để tránh trùng lặp
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

            // Sử dụng prefab được kéo thả vào Inspector, nếu rỗng thì thử load fallback từ Resources
            var prefabToUse = buttonPrefab != null ? buttonPrefab : Resources.Load<GameObject>("Prefabs/UI/Component/Button");
            if (prefabToUse == null)
            {
                Debug.LogError("[UI_ConfirmPanel] Không tìm thấy prefab Button để khởi tạo!");
                return;
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                var btnInfo = buttons[i];
                var btnGo = Instantiate(prefabToUse, container);
                btnGo.name = $"DynamicButton_{i}_{btnInfo.text}";

                var button = btnGo.GetComponent<Button>();
                var txtComp = btnGo.GetComponentInChildren<TMP_Text>();

                if (txtComp != null)
                {
                    txtComp.text = btnInfo.text;
                }

                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        Hide();
                        btnInfo.callback?.Invoke();
                        DestroyPopup();
                    });
                }
            }
        }

        private void DestroyPopup()
        {
            Destroy(gameObject, 0.5f);
        }
    }
}
