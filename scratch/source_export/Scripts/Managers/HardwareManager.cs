using UnityEngine;

namespace GameClient.Managers
{
    public static class HardwareManager
    {
        public static void AutoDetectAndApplySettings()
        {
            var device = DeviceManager.Instance;
            int systemRam = SystemInfo.systemMemorySize;
            int processorCount = SystemInfo.processorCount;

            Debug.Log($"[Hardware] OS: {device.GetOS()} | RAM: {systemRam}MB | Cores: {processorCount}");

            if (device.IsLowEndDevice())
            {
                ApplyLowSettings();
            }
            else if (systemRam >= 6000 || !device.IsMobile)
            {
                ApplyHighSettings();
            }
            else
            {
                ApplyMediumSettings();
            }

            if (device.IsAndroid)
            {
                QualitySettings.shadowDistance = device.IsLowEndDevice() ? 15f : 40f;
            }
        }

        private static void ApplyHighSettings()
        {
            Application.targetFrameRate = 60;
            QualitySettings.SetQualityLevel(2, true);
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 4;
            QualitySettings.globalTextureMipmapLimit = 0; // Full Resolution
            Debug.Log("[Hardware] Preset: HIGH (60 FPS, 4x AA)");
        }

        private static void ApplyMediumSettings()
        {
            Application.targetFrameRate = 45;
            QualitySettings.SetQualityLevel(1, true);
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 2;
            QualitySettings.globalTextureMipmapLimit = 1; // Half Resolution
            Debug.Log("[Hardware] Preset: MEDIUM (45 FPS, 2x AA, Half Texture)");
        }

        private static void ApplyLowSettings()
        {
            Application.targetFrameRate = 30;
            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.globalTextureMipmapLimit = 2; // Quarter Resolution
            
            if (DeviceManager.Instance.IsMobile) {
                QualitySettings.resolutionScalingFixedDPIFactor = 0.85f;
            }
            
            Debug.Log("[Hardware] Preset: LOW (30 FPS, No AA, Scale 0.85, Quarter Texture)");
        }
    }
}
