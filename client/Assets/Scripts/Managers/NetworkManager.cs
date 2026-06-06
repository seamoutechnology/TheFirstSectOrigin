using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using GameClient.Core;
using GameClient.Managers;
using GameClient.Network.Pb;
using System.Net.Http;

namespace GameClient.Network
{
    public class NetworkManager : Singleton<NetworkManager>
    {
        private GrpcChannel _loginChannel;
        private GrpcChannel _gatewayChannel;
        
        public bool IsConnected { get; private set; }
        public bool IsReconnecting { get; private set; }
        
        private string _lastHost;
        private int _lastPort;

        public GatewayService.GatewayServiceClient GatewayClient { get; private set; }
        public AuthService.AuthServiceClient AuthClient { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            ConnectToDefaultGateway();
        }

        private void OnApplicationQuit()
        {
            ShutdownGrpcChannel();
        }

        private void OnDestroy()
        {
            ShutdownGrpcChannel();
        }

        private void ShutdownGrpcChannel()
        {
            if (_loginChannel != null)
            {
                var ch = _loginChannel;
                _loginChannel = null;
                Task.Run(() => {
                    try { ch.Dispose(); } catch (Exception) { }
                });
            }
            if (_gatewayChannel != null)
            {
                var ch = _gatewayChannel;
                _gatewayChannel = null;
                Task.Run(() => {
                    try { ch.Dispose(); } catch (Exception) { }
                });
            }
        }

        public void ConnectToDefaultGateway()
        {
            string addr = GameSettings.Instance?.gatewayAddr;
            if (string.IsNullOrEmpty(addr)) addr = "127.0.0.1:" + GameConstants.Network.DEFAULT_PORT;

            // Loại bỏ http:// nếu có (Grpc.Core không cần prefix)
            addr = addr.Replace("http://", "").Replace("https://", "");

            string[] parts = addr.Split(':');
            string host = parts[0];
            if (host.ToLower() == "localhost") host = "127.0.0.1"; // Tránh lỗi IPv6 loopback của Grpc.Core trên Windows
            int port = GameConstants.Network.DEFAULT_PORT;
            if (parts.Length > 1) int.TryParse(parts[1], out port);

            ConnectToGateway(host, port);
        }

        public void ConnectToGateway(string address)
        {
            address = address.Replace("http://", "").Replace("https://", "");
            string[] parts = address.Split(':');
            string host = parts[0];
            if (host.ToLower() == "localhost") host = "127.0.0.1"; // Tránh lỗi IPv6 loopback của Grpc.Core trên Windows
            int port = GameConstants.Network.DEFAULT_PORT;
            if (parts.Length > 1) int.TryParse(parts[1], out port);
            ConnectToGateway(host, port);
        }

        public void ConnectToGateway(string host, int port)
        {
            _lastHost = host;
            _lastPort = port;
            IsReconnecting = false;

            Debug.Log($"<color=cyan>[Network] Đang kết nối tới gRPC Host: {host}, Port: {port}...</color>");

            try
            {
                if (_loginChannel != null)
                {
                    _loginChannel.Dispose();
                }
                if (_gatewayChannel != null)
                {
                    _gatewayChannel.Dispose();
                }

                var handler = new HttpClientHandler();
                handler.UseProxy = false; // QUAN TRỌNG: Sửa lỗi Unity bị đơ 10-15s ở lần kết nối đầu tiên trên Windows do Auto-Proxy
                var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, handler);

                int loginPort = GameClient.Core.GameConstants.Network.LOGIN_PORT;
                _loginChannel = GrpcChannel.ForAddress($"http://{host}:{loginPort}", new GrpcChannelOptions {
                    HttpHandler = httpHandler,
                    Credentials = ChannelCredentials.Insecure
                });

                int gatewayPort = port;
                _gatewayChannel = GrpcChannel.ForAddress($"http://{host}:{gatewayPort}", new GrpcChannelOptions {
                    HttpHandler = httpHandler,
                    Credentials = ChannelCredentials.Insecure
                });
                
                GatewayClient = new GatewayService.GatewayServiceClient(_gatewayChannel);
                AuthClient = new AuthService.AuthServiceClient(_loginChannel);

                Debug.Log($"<color=green>[Network] Channel gRPC đã sẵn sàng tại {host}:{port}</color>");
                
                IsConnected = true;
                
                EventManager.Instance.Emit(GameEvents.ON_SERVER_CONNECTED);
            }
            catch (Exception e)
            {
                IsConnected = false;
                Debug.LogError($"[Network] Lỗi khởi tạo Channel: {e.Message}");
                TriggerReconnect();
            }
        }

        public void HandleConnectionError(Exception ex)
        {
            if (IsReconnecting) return;
            
            Debug.LogWarning($"[Network] Phát hiện đứt kết nối: {ex.Message}");
            IsConnected = false;
            TriggerReconnect();
        }

        private void TriggerReconnect()
        {
            if (IsReconnecting) return;
            IsReconnecting = true;
            IsConnected = false;
            
            EventManager.Instance.Emit("ON_SERVER_DISCONNECTED");
            
            StartCoroutine(ReconnectRoutine());
        }

        private IEnumerator ReconnectRoutine()
        {
            Debug.Log("[Network] Bắt đầu tự động kết nối lại (Auto-Reconnect)...");
            
            int attempts = 0;
            int maxAttempts = 5;
            
            while (!IsConnected && attempts < maxAttempts)
            {
                attempts++;
                Debug.Log($"[Network] Reconnect attempt {attempts}/{maxAttempts}...");
                
                ConnectToGateway(_lastHost, _lastPort);
                
                if (IsConnected)
                {
                    Debug.Log("<color=green>[Network] Kết nối lại thành công!</color>");
                    EventManager.Instance.Emit("ON_SERVER_RECONNECTED");
                    yield break;
                }
                
                yield return new WaitForSeconds(3f); // Đợi 3s trước khi thử lại
            }
            
            Debug.LogError("[Network] Kết nối lại thất bại sau nhiều lần thử. Vui lòng kiểm tra lại mạng!");
            IsReconnecting = false;
            EventManager.Instance.Emit("ON_SERVER_RECONNECT_FAILED");
        }

        public async Task<string> PostAsync(string url, object data)
        {
            if (url.StartsWith("/") && GameSettings.Instance != null)
            {
                url = GameSettings.Instance.apiBaseUrl.TrimEnd('/') + url;
            }

            Debug.Log($"[Network] POST → {url}");

            string json = JsonUtility.ToJson(data);
            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                
                string token = AccountManager.Instance.CurrentToken;
                if (!string.IsNullOrEmpty(token)) 
                {
                    www.SetRequestHeader("Authorization", GameConstants.Network.BEARER_PREFIX + token);
                }

                // this==null check gây thoát vòng lặp sớm → result=InProgress, httpCode=0.
                var tcs = new TaskCompletionSource<bool>();
                var op = www.SendWebRequest();
                op.completed += _ => tcs.TrySetResult(true);

                int timeoutMs = (int)(GameConstants.Network.REQUEST_TIMEOUT * 1000);
                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));

                if (completedTask != tcs.Task)
                {
                    Debug.LogError($"[Network] Timeout ({GameConstants.Network.REQUEST_TIMEOUT}s) tại {url}");
                    www.Abort();
                    return null;
                }

                string responseText = www.downloadHandler?.text ?? "";
                long responseCode = www.responseCode;
                
                Debug.Log($"[Network] ← {url}: result={www.result}, httpCode={responseCode}");
                Debug.Log($"[Network] Raw JSON: {responseText}");

                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (responseCode >= 200 && responseCode < 300 && !string.IsNullOrEmpty(responseText))
                    {
                        Debug.LogWarning($"[Network] Unity mark lỗi nhưng server trả về {responseCode}. Hint: {www.error}");
                        return responseText;
                    }
                    Debug.LogError($"[HTTP Error] result={www.result}, error='{www.error}', httpCode={responseCode} at {url}");
                    return null;
                }
                
                return responseText;
            }
        }

        public async Task<T> PostAsync<T>(string url, object data) where T : class
        {
            string response = await PostAsync(url, data);
            if (string.IsNullOrEmpty(response)) return null;
            try
            {
                string jsonResp = response.Trim();
                if (jsonResp.StartsWith("["))
                {
                    jsonResp = "{ \"data\": " + jsonResp + " }";
                }
                return JsonUtility.FromJson<T>(jsonResp);
            }
            catch (Exception e)
            {
                Debug.LogError($"[JSON Error] {e.Message}\nRaw JSON: {response}");
                return null;
            }
        }

        public static CallOptions DefaultCallOptions()
        {
            var headers = new Metadata();
            string token = AccountManager.Instance.CurrentToken;
            if (!string.IsNullOrEmpty(token)) headers.Add("authorization", $"{GameConstants.Network.BEARER_PREFIX}{token}");
            
            return new CallOptions(headers).WithDeadline(DateTime.UtcNow.AddSeconds(10));
        }
    }
}
