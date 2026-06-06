using System;
using System.Collections.Generic;
using UnityEngine;
using GameClient.Core;

namespace GameClient.Chat
{
    public enum ChatChannel
    {
        Global,   // Toàn Server
        Guild,    // Trong Bang Hội
        Private   // Mật Ngữ (1-1)
    }

    [Serializable]
    public class ChatMessage
    {
        public string senderId;
        public string senderName;
        public string content;
        public ChatChannel channel;
        public long timestamp;
        
        public string targetId;
    }

    public class ChatManager : Singleton<ChatManager>
    {
        public event Action<ChatMessage> OnMessageReceived;

        private Dictionary<ChatChannel, List<ChatMessage>> _messageHistory = new Dictionary<ChatChannel, List<ChatMessage>>();

        protected override void Awake()
        {
            base.Awake();
            foreach (ChatChannel channel in System.Enum.GetValues(typeof(ChatChannel)))
            {
                _messageHistory[channel] = new List<ChatMessage>();
            }
        }

        public void SendMessage(ChatChannel channel, string content, string targetId = "")
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            // TODO: Gửi qua TCP/WebSocket (ví dụ: NetworkClient.Send(new ChatPacket(...)))
            Debug.Log($"[ChatManager] Gửi tin lên kênh {channel}: {content}");

            var msg = new ChatMessage
            {
                senderId = "my_id", // Lấy từ PlayerProfile
                senderName = "Người Chơi (Tôi)",
                content = content,
                channel = channel,
                targetId = targetId,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            
            ReceiveMessageFromServer(msg);
        }

        public void ReceiveMessageFromServer(ChatMessage msg)
        {
            if (_messageHistory.TryGetValue(msg.channel, out var history))
            {
                history.Add(msg);
                
                if (history.Count > 100)
                {
                    history.RemoveAt(0);
                }
            }

            OnMessageReceived?.Invoke(msg);
        }

        public List<ChatMessage> GetHistory(ChatChannel channel)
        {
            return _messageHistory.TryGetValue(channel, out var list) ? list : new List<ChatMessage>();
        }
    }
}
