using UnityEngine;
using TFSO.Managers;
using System.Collections;
using GameClient.Managers;
using GameClient.Network;
using TFSO.Core;

namespace TFSO.Tests
{
    public class SystemIntegrationTest : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("--- Bắt đầu chạy Integration Test Client ---");
            TestSingleton();
            TestLocalization();
            TestObjectPool();
            TestDevice();
            Debug.Log("--- Integration Test Client Hoàn tất ---");
        }

        private void TestSingleton()
        {
            var net = NetworkManager.Instance;
            if (net != null) Debug.Log("[PASS] Singleton NetworkManager hoạt động.");
        }

        private void TestLocalization()
        {
            string mockJson = "{\"msg_success\": \"Thành công rực rỡ\", \"msg_error\": \"Lỗi rồi sư phụ ơi\"}";
            LocalizationManager.Instance.LoadLanguage(mockJson);
            string result = LocalizationManager.Instance.GetText("msg_success");
            if (result == "Thành công rực rỡ") Debug.Log("[PASS] Localization load và dịch đúng.");
            else Debug.LogError("[FAIL] Localization dịch sai: " + result);
        }

        private void TestObjectPool()
        {
            GameObject dummy = new GameObject("TestPrefab");
            GameObject spawned = ObjectPool.Instance.Get(dummy);
            if (spawned != null)
            {
                ObjectPool.Instance.ReturnToPool(spawned);
                Debug.Log("[PASS] ObjectPool Get/Return hoạt động.");
            }
            Destroy(dummy);
        }

        private void TestDevice()
        {
            Debug.Log($"[INFO] Device OS: {DeviceManager.Instance.GetOS()}");
            Debug.Log($"[INFO] Device Memory: {DeviceManager.Instance.GetMemorySize()}MB");
        }
    }
}
