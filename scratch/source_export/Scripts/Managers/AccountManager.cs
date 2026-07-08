using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using GameClient.Core;
using GameClient.Network;
using GameClient.Network.Pb;
using GameClient.Utils;
using Grpc.Core;

namespace GameClient.Managers
{
    [Serializable]
    public class LocalAccountData
    {
        public string Username;
        public string EncryptedPassword;
        public long LastLoginTimestamp;
    }

    [Serializable]
    public class AccountHistory
    {
        public List<LocalAccountData> Accounts = new List<LocalAccountData>();
    }

    public class AccountManager : Singleton<AccountManager>
    {
        private const string PREFS_ACCOUNT_HISTORY = "AccountHistory_v1";


        private AccountHistory _history;
        public string CurrentToken { get; private set; }
        public string CurrentUsername { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            LoadHistory();
            CurrentToken = PlayerPrefs.GetString(GameClient.Core.GameConstants.PlayerPrefsKeys.TOKEN, "");
            
            var lastAcc = GetLastAccount();
            
            // auto login lụm lun nha
            if (lastAcc != null && !string.IsNullOrEmpty(CurrentToken))
            {
                CurrentUsername = lastAcc.Username;
            }
        }

        private void LoadHistory()
        {
            string json = PlayerPrefs.GetString(PREFS_ACCOUNT_HISTORY, "");
            if (string.IsNullOrEmpty(json))
            {
                _history = new AccountHistory();
            }
            else
            {
                try
                {
                    _history = JsonUtility.FromJson<AccountHistory>(json);
                }
                catch
                {
                    _history = new AccountHistory();
                }
            }
        }

        private void SaveHistory()
        {
            string json = JsonUtility.ToJson(_history);
            PlayerPrefs.SetString(PREFS_ACCOUNT_HISTORY, json);
            PlayerPrefs.Save();
        }

        public List<LocalAccountData> GetSavedAccounts()
        {
            return _history.Accounts.OrderByDescending(a => a.LastLoginTimestamp).ToList();
        }

        public LocalAccountData GetLastAccount()
        {
            var accounts = GetSavedAccounts();
            if (accounts.Count > 0) return accounts[0];
            return null;
        }

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            if (NetworkManager.Instance == null || NetworkManager.Instance.AuthClient == null)
            {
                NetworkManager.Instance.ConnectToDefaultGateway();
            }

            var request = new LoginRequest { Username = username, Password = password };
            
            try
            {
                var options = NetworkManager.DefaultCallOptions();
                var response = await NetworkManager.Instance.AuthClient.LoginAsync(request, options);

                if (response != null && response.Code == 0 && !string.IsNullOrEmpty(response.Token))
                {
                    SetCurrentSession(username, response.Token);
                    SaveAccountToHistory(username, password);
                }

                return response;
            }
            catch (RpcException ex)
            {
                Debug.LogError($"[AccountManager] gRPC Login Error: {ex.Status}");
                throw;
            }
        }

        private void SetCurrentSession(string username, string token)
        {
            CurrentUsername = username;
            CurrentToken = token;
            PlayerPrefs.SetString(GameClient.Core.GameConstants.PlayerPrefsKeys.TOKEN, token);
            PlayerPrefs.Save();
        }

        private void SaveAccountToHistory(string username, string password)
        {
            var existing = _history.Accounts.FirstOrDefault(a => a.Username == username);
            if (existing != null)
            {
                existing.EncryptedPassword = CryptoUtils.Encrypt(password);
                existing.LastLoginTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            else
            {
                _history.Accounts.Add(new LocalAccountData
                {
                    Username = username,
                    EncryptedPassword = CryptoUtils.Encrypt(password),
                    LastLoginTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
            }

            // giữ tối đa 5 acc thôi ko nặng máy :v
            // giữ 5 acc thôi ko nặng máy :v
            if (_history.Accounts.Count > 5)
            {
                _history.Accounts = _history.Accounts
                    .OrderByDescending(a => a.LastLoginTimestamp)
                    .Take(5)
                    .ToList();
            }

            SaveHistory();
        }

        public void Logout()
        {
            CurrentToken = "";
            CurrentUsername = "";
            PlayerPrefs.DeleteKey(GameClient.Core.GameConstants.PlayerPrefsKeys.TOKEN);
            PlayerPrefs.Save();
            
        }
    }
}
