using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TFSO.Core; // Assuming Singleton is in here
using GameClient.Managers; // Assuming MapManager is here

namespace GameClient.UI
{
    public class DeepLinkManager : Singleton<DeepLinkManager>
    {
        private Queue<string> _pendingLinks = new Queue<string>();

        public bool IsGameReady { get; set; } = false;

        protected override void Awake()
        {
            base.Awake();
            
            Application.deepLinkActivated += OnDeepLinkActivated;

            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                OnDeepLinkActivated(Application.absoluteURL);
            }
        }

        private void OnDestroy()
        {
            Application.deepLinkActivated -= OnDeepLinkActivated;
        }

        private void OnDeepLinkActivated(string url)
        {
            Debug.Log($"[DeepLink] Activated: {url}");
            
            string command = url;
            if (url.StartsWith("tfso://"))
            {
                command = url.Substring("tfso://".Length);
            }

            _pendingLinks.Enqueue(command);

            if (IsGameReady)
            {
                ProcessPendingLinks();
            }
        }

        public void EnqueueInternalLink(string innerLink)
        {
            _pendingLinks.Enqueue(innerLink);
            if (IsGameReady)
            {
                ProcessPendingLinks();
            }
        }

        public void ProcessPendingLinks()
        {
            while (_pendingLinks.Count > 0)
            {
                string link = _pendingLinks.Dequeue();
                ExecuteLink(link);
            }
        }

        private void ExecuteLink(string link)
        {
            Debug.Log($"[DeepLink] Executing: {link}");

            string[] parts = link.Split('/');
            if (parts.Length < 2) return;

            string category = parts[0].ToLower();
            string targetId = parts[1];

            switch (category)
            {
                case "item":
                    Debug.Log($"[DeepLink] Request to open item details for: {targetId}");
                    // TODO: UIManager.Instance.OpenPanel<InventoryPanel>().ShowItem(targetId);
                    break;
                case "event":
                    Debug.Log($"[DeepLink] Request to open event panel for: {targetId}");
                    break;
                case "building":
                    Debug.Log($"[DeepLink] Request to focus building: {targetId}");
                    break;
                case "gacha":
                    Debug.Log($"[DeepLink] Request to open gacha: {targetId}");
                    break;
                default:
                    Debug.LogWarning($"[DeepLink] Unknown link category: {category}");
                    break;
            }
        }
    }
}
