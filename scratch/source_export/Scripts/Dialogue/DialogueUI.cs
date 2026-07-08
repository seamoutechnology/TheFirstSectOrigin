using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using GameClient.Managers; // Lấy InputManager

namespace GameClient.Dialogue
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public GameObject panelRoot;
        public TMP_Text txtSpeakerName;
        public TMP_Text txtContent;
        public Image imgAvatarLeft;
        public Image imgAvatarRight;

        [Header("Settings")]
        public float typeSpeed = 0.05f; // Tốc độ gõ chữ

        private DialogueNode _currentNode;
        private bool _isTyping = false;
        private Tween _typewriterTween;
        private Coroutine _autoCoroutine;

        [Header("Colors (Focus/Unfocus)")]
        public Color colorFocus = Color.white;
        public Color colorUnfocus = new Color(0.4f, 0.4f, 0.4f, 1f);

        private void Start()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnNodeStarted += PlayNode;
                DialogueManager.Instance.OnDialogueEnded += HideDialogue;
            }

            if (InputManager.Instance != null)
            {
                InputManager.Instance.RegisterTap("NextDialogue", UnityEngine.InputSystem.Key.Space, OnNextClicked);
            }

            var bgBtn = panelRoot.GetComponent<Button>();
            if (bgBtn != null) bgBtn.onClick.AddListener(OnNextClicked);

            HideDialogue();
        }

        private void OnDestroy()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnNodeStarted -= PlayNode;
                DialogueManager.Instance.OnDialogueEnded -= HideDialogue;
            }
            if (InputManager.Instance != null)
            {
                InputManager.Instance.Unregister("NextDialogue");
            }
        }

        public void PlayNode(DialogueNode node)
        {
            if (node == null) return;
            _currentNode = node;
            panelRoot.SetActive(true);

            if (_autoCoroutine != null) StopCoroutine(_autoCoroutine);

            string localizedName = DialogueManager.Instance.GetLocalizedText(node.speakerNameKey);
            string localizedContent = DialogueManager.Instance.GetLocalizedText(node.textKey);

            txtSpeakerName.text = localizedName;

            imgAvatarLeft.gameObject.SetActive(true);
            imgAvatarRight.gameObject.SetActive(true);

            if (node.position == DialoguePosition.Left)
            {
                imgAvatarLeft.DOColor(colorFocus, 0.2f);
                imgAvatarRight.DOColor(colorUnfocus, 0.2f);
                imgAvatarLeft.transform.SetAsLastSibling(); // Hiện lên trên
            }
            else if (node.position == DialoguePosition.Right)
            {
                imgAvatarLeft.DOColor(colorUnfocus, 0.2f);
                imgAvatarRight.DOColor(colorFocus, 0.2f);
                imgAvatarRight.transform.SetAsLastSibling();
            }
            else // Center/Narrator
            {
                imgAvatarLeft.DOColor(colorUnfocus, 0.2f);
                imgAvatarRight.DOColor(colorUnfocus, 0.2f);
            }

            // TODO: Generate Choices Buttons if node.choices.Count > 0

            txtContent.text = "";
            _isTyping = true;
            _typewriterTween?.Kill(); // Dừng các tween khác nếu có

            if (DialogueManager.Instance.IsSkipMode)
            {
                txtContent.text = localizedContent;
                _isTyping = false;
                OnTypingComplete();
            }
            else
            {
                if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = StartCoroutine(TypewriterRoutine(localizedContent));
            }
        }

        private Coroutine _typewriterCoroutine;

        private IEnumerator TypewriterRoutine(string content)
        {
            float delay = typeSpeed * DialogueManager.Instance.SpeedMultiplier;
            for (int i = 0; i < content.Length; i++)
            {
                txtContent.text += content[i];
                yield return new WaitForSeconds(delay);
            }
            _isTyping = false;
            OnTypingComplete();
        }

        private void OnTypingComplete()
        {
            if (DialogueManager.Instance.IsAutoMode && _currentNode.choices.Count == 0)
            {
                _autoCoroutine = StartCoroutine(AutoNextRoutine());
            }
        }

        private IEnumerator AutoNextRoutine()
        {
            yield return new WaitForSeconds(1.5f); // Đợi 1.5s trước khi nhảy câu tiếp
            DialogueManager.Instance.NextNode();
        }

        public void HideDialogue()
        {
            panelRoot.SetActive(false);
            _currentNode = null;
            if (_autoCoroutine != null) StopCoroutine(_autoCoroutine);
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
        }

        public void OnNextClicked()
        {
            if (!panelRoot.activeSelf || _currentNode == null) return;

            if (_isTyping)
            {
                if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
                txtContent.text = DialogueManager.Instance.GetLocalizedText(_currentNode.textKey);
                _isTyping = false;
                OnTypingComplete();
            }
            else
            {
                if (_currentNode.choices.Count == 0)
                {
                    DialogueManager.Instance.NextNode();
                }
            }
        }
    }
}
