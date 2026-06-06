using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using GameClient.Managers;

namespace GameClient.UI
{
    public class UISoundTrigger : MonoBehaviour, IPointerEnterHandler
    {
        [Header("Audio Keys (Addressables)")]
        public string clickSFX = GameClient.Core.GameConstants.Audio.SFX_CLICK;
        public string hoverSFX = GameClient.Core.GameConstants.Audio.SFX_HOVER;
        public string typeSFX = GameClient.Core.GameConstants.Audio.SFX_TYPE;

        [Header("Settings")]
        public bool playOnHover = true;
        public bool playOnClick = true;
        public bool playOnType = true;

        private Button _button;
        private TMP_InputField _inputField;

        void Awake()
        {
            _button = GetComponent<Button>();
            _inputField = GetComponent<TMP_InputField>();
        }

        void Start()
        {
            if (playOnClick && _button != null)
            {
                _button.onClick.AddListener(PlayClickSound);
            }

            if (playOnType && _inputField != null)
            {
                _inputField.onValueChanged.AddListener(PlayTypeSound);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (playOnHover && (_button != null && _button.interactable))
            {
                AudioManager.Instance.PlaySFX(hoverSFX);
            }
        }

        private void PlayClickSound()
        {
            if (!string.IsNullOrEmpty(clickSFX))
                AudioManager.Instance.PlaySFX(clickSFX);
        }

        private void PlayTypeSound(string value)
        {
            if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(typeSFX))
            {
                AudioManager.Instance.PlaySFX(typeSFX);
            }
        }


        public static void AddToAll(GameObject root)
        {
            if (root == null) return;

            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.GetComponent<UISoundTrigger>() == null)
                    btn.gameObject.AddComponent<UISoundTrigger>();
            }

            var inputs = root.GetComponentsInChildren<TMP_InputField>(true);
            foreach (var input in inputs)
            {
                if (input.GetComponent<UISoundTrigger>() == null)
                    input.gameObject.AddComponent<UISoundTrigger>();
            }
        }
    }
}
