using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using GameClient.Core;
using GameClient.Network;

namespace GameClient.Managers
{
    public class CutsceneSyncManager : Singleton<CutsceneSyncManager>
    {
        public async Task<bool> DownloadCutsceneAsync(string cutsceneId)
        {
            string savePath = Path.Combine(Application.persistentDataPath, $"{cutsceneId}.json");
            string url = $"{NetworkConfig.BASE_URL}/api/v1/cutscenes/{cutsceneId}";
            
            Debug.Log($"[CutsceneSync] Đang tải kịch bản {cutsceneId} từ {url} ...");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    File.WriteAllText(savePath, request.downloadHandler.text);
                    Debug.Log($"[CutsceneSync] Đã tải và lưu kịch bản {cutsceneId} thành công!");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[CutsceneSync] Tải kịch bản từ Server thất bại ({request.responseCode}). Đang load Fallback local...");
                }
            }

            TextAsset localFallback = GameClient.Core.ResourceManager.Instance.LoadFromResources<TextAsset>($"Cutscenes/{cutsceneId}");
            if (localFallback != null)
            {
                File.WriteAllText(savePath, localFallback.text);
                Debug.Log($"[CutsceneSync] Đã lưu kịch bản {cutsceneId} từ Local Fallback!");
                return true;
            }

            Debug.LogError($"[CutsceneSync] Không tìm thấy kịch bản {cutsceneId} cả ở Server lẫn Local!");
            return false;
        }

        public string GetCutsceneJson(string cutsceneId)
        {
            string loadPath = Path.Combine(Application.persistentDataPath, $"{cutsceneId}.json");
            if (File.Exists(loadPath))
            {
                return File.ReadAllText(loadPath);
            }
            
            TextAsset localFallback = GameClient.Core.ResourceManager.Instance.LoadFromResources<TextAsset>($"Cutscenes/{cutsceneId}");
            if (localFallback != null) return localFallback.text;

            return string.Empty;
        }
    }
}
