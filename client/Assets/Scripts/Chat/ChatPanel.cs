using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameClient.Chat
{
    public class ChatPanel : MonoBehaviour
    {
        [Header("Tabs")]
        public Button btnGlobalTab;
        public Button btnGuildTab;
        public Button btnPrivateTab;

        [Header("UI Elements")]
        public TMP_Text txtChatLog;
        public TMP_InputField inputField;
        public Button btnSend;

        private ChatChannel _currentChannel = ChatChannel.Global;

        private void Start()
        {
            if (ChatManager.Instance != null)
            {
                ChatManager.Instance.OnMessageReceived += OnNewMessage;
            }

            btnGlobalTab?.onClick.AddListener(() => SwitchTab(ChatChannel.Global));
            btnGuildTab?.onClick.AddListener(() => SwitchTab(ChatChannel.Guild));
            btnPrivateTab?.onClick.AddListener(() => SwitchTab(ChatChannel.Private));

            btnSend?.onClick.AddListener(OnSendClicked);

            SwitchTab(ChatChannel.Global);
        }

        private void OnDestroy()
        {
            if (ChatManager.Instance != null)
            {
                ChatManager.Instance.OnMessageReceived -= OnNewMessage;
            }
        }

        private void SwitchTab(ChatChannel newChannel)
        {
            _currentChannel = newChannel;
            Debug.Log($"[ChatPanel] Chuyển sang Tab: {newChannel}");

            // TODO: Đổi màu nút Tab đang được chọn (UI Feedback)

            RefreshChatLog();
        }

        private void RefreshChatLog()
        {
            if (txtChatLog == null || ChatManager.Instance == null) return;

            txtChatLog.text = ""; // Xóa log cũ
            
            var history = ChatManager.Instance.GetHistory(_currentChannel);
            foreach (var msg in history)
            {
                AppendMessageToLog(msg);
            }
        }

        private void OnNewMessage(ChatMessage msg)
        {
            if (msg.channel == _currentChannel)
            {
                AppendMessageToLog(msg);
            }
        }

        private void AppendMessageToLog(ChatMessage msg)
        {
            if (txtChatLog == null) return;
            
            string colorHex = "#FFFFFF";
            if (msg.channel == ChatChannel.Global) colorHex = "#FFA500"; // Cam
            if (msg.channel == ChatChannel.Guild) colorHex = "#00FF00";  // Xanh lá
            if (msg.channel == ChatChannel.Private) colorHex = "#FF69B4"; // Hồng

            string formattedMsg = $"<color={colorHex}>[{msg.senderName}]</color>: {msg.content}\n";
            txtChatLog.text += formattedMsg;
        }

        private void OnSendClicked()
        {
            if (inputField == null || ChatManager.Instance == null) return;

            string content = inputField.text;
            if (!string.IsNullOrEmpty(content))
            {
                ChatManager.Instance.SendMessage(_currentChannel, content, "TargetID_NeuLaPrivate");
                
                inputField.text = "";
            }
        }
    }
}
