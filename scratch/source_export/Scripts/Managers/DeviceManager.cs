using UnityEngine;

namespace GameClient.Managers
{
    public class DeviceManager : Singleton<DeviceManager>
    {
        public string GetDeviceId() => SystemInfo.deviceUniqueIdentifier;
        public string GetDeviceModel() => SystemInfo.deviceModel;
        public int GetMemorySize() => SystemInfo.systemMemorySize;
        public string GetOS() => SystemInfo.operatingSystem;

        public bool IsMobile => Application.isMobilePlatform;
        public bool IsAndroid => Application.platform == RuntimePlatform.Android;
        public bool IsIos => Application.platform == RuntimePlatform.IPhonePlayer;
        public bool IsEditor => Application.isEditor;

        public bool IsLowEndDevice()
        {
            if (IsMobile) return SystemInfo.systemMemorySize < 3000;
            return SystemInfo.systemMemorySize < 4096;
        }

        public void SetTargetFPS(int fps)
        {
            Application.targetFrameRate = fps;
        }

        public float GetBatteryLevel() => SystemInfo.batteryLevel;
        public BatteryStatus GetBatteryStatus() => SystemInfo.batteryStatus;
    }
}
