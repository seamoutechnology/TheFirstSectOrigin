using UnityEngine;
using UnityEngine.UI;
using GameClient.Managers;
using GameClient.Core;
using GameClient.Core.Interfaces;
using GameClient.Network;
using TMPro;
using System.Threading.Tasks;
using VContainer;
using GameClient.UI.Presenters;
using System;

namespace GameClient.UI
{
    public class EntryPanel : MonoBehaviour, IUIView, IEntryView
    {
        [Inject] private LocalizationManager _localization;
        [Inject] private UIManager _uiManager;
        [Inject] private NetworkManager _network;
        [Header("Top Left Info")]
        public TMP_Text txtVersion;

        [Header("Left Buttons")]
        public Button btnNotice;

        [Header("Center Server Info")]
        public TMP_Text txtServerName;
        public Image imgServerStatus;
        public Sprite statusNeutral;
        public Sprite statusOnline;
        public Sprite statusOffline;
        public Button btnChangeServer;
        public Button btnEnterGame;

        public event Action OnNoticeClicked;
        public event Action OnChangeServerClicked;
        public event Action OnEnterGameClicked;

        private EntryPresenter _presenter;

        public bool IsVisible => gameObject.activeSelf;

        public void Setup(object data = null)
        {
            if (_localization == null) _localization = LocalizationManager.Instance;
            if (_uiManager == null) _uiManager = UIManager.Instance;
            if (_network == null) _network = NetworkManager.Instance;

            if (_presenter == null)
            {
                _presenter = new EntryPresenter(this, _localization, _uiManager, _network);
            }
            _presenter.Initialize();

            if (btnNotice != null)
            {
                btnNotice.onClick.RemoveAllListeners();
                btnNotice.onClick.AddListener(() => OnNoticeClicked?.Invoke());
            }

            if (btnChangeServer != null)
            {
                btnChangeServer.onClick.RemoveAllListeners();
                btnChangeServer.onClick.AddListener(() => OnChangeServerClicked?.Invoke());
            }

            if (btnEnterGame != null)
            {
                btnEnterGame.onClick.RemoveAllListeners();
                btnEnterGame.onClick.AddListener(() => OnEnterGameClicked?.Invoke());
            }
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
        }

        public void SetVersionText(string text)
        {
            if (txtVersion != null) txtVersion.text = text;
        }

        public void SetServerInfo(string serverName, bool isOnline, Sprite sOnline, Sprite sOffline, Sprite sNeutral)
        {
            if (txtServerName != null) txtServerName.text = serverName;
            
            if (imgServerStatus != null)
            {
                if (isOnline && (sOnline != null || statusOnline != null))
                    imgServerStatus.sprite = sOnline ?? statusOnline;
                else if (!isOnline && (sOffline != null || statusOffline != null))
                    imgServerStatus.sprite = sOffline ?? statusOffline;
                else
                    imgServerStatus.color = isOnline ? Color.green : Color.red; // Fallback
            }
        }

        public void SetEnterButtonInteractable(bool interactable)
        {
            if (btnEnterGame != null) btnEnterGame.interactable = interactable;
        }

        public void ShowNoticePanel()
        {
            _uiManager.OpenPanel("NoticePanel");
        }

        public void ShowZoneSelectPanel()
        {
            _uiManager.OpenPanel("ZoneSelectPanel");
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
