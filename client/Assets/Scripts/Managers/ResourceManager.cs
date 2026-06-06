using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using GameClient.Core.Interfaces;

namespace GameClient.Core
{
    public class ResourceManager : Singleton<ResourceManager>, GameClient.Core.Interfaces.IResourceManager
    {
        public async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<T>(address);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result;
                }
                
                LogError($"Không thể tải asset: {address}");
                return null;
            }
            catch (Exception ex)
            {
                LogError($"Lỗi Addressable load {address}: {ex.Message}");
                return null;
            }
        }

        public T LoadFromResources<T>(string path) where T : UnityEngine.Object
        {
            try
            {
                var asset = Resources.Load<T>(path);
                if (asset != null) return asset;
                
                LogError($"Không thể tải asset từ Resources: {path}");
                return null;
            }
            catch (Exception ex)
            {
                LogError($"Lỗi LoadFromResources {path}: {ex.Message}");
                return null;
            }
        }

        /// <summary>Giải phóng Asset ra khỏi RAM</summary>
        public void ReleaseAsset(object asset)
        {
            if (asset == null) return;
            Addressables.Release(asset);
        }

        /// <summary>Xóa GameObject và giải phóng bộ nhớ Addressables</summary>
        public void ReleaseInstance(UnityEngine.GameObject instance)
        {
            if (instance == null) return;
            Addressables.ReleaseInstance(instance);
        }

        public async Task<GameObject> InstantiateAsync(string address, Transform parent = null)
        {
            try
            {
                var handle = Addressables.InstantiateAsync(address, parent);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result;
                }
                
                LogError($"Không thể Instantiate: {address}");
                return null;
            }
            catch (Exception ex)
            {
                LogError($"Lỗi Addressable Instantiate {address}: {ex.Message}");
                return null;
            }
        }

        public async Task<GameObject> InstantiateAsync(string address, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            try
            {
                var handle = Addressables.InstantiateAsync(address, position, rotation, parent);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result;
                }
                
                LogError($"Không thể Instantiate tại vị trí: {address}");
                return null;
            }
            catch (Exception ex)
            {
                LogError($"Lỗi Addressable Instantiate {address}: {ex.Message}");
                return null;
            }
        }

        private void LogError(string msg) => Debug.LogError($"[ResourceManager] {msg}");
    }
}
