using System;
using System.Collections.Generic;
using UnityEngine;
using GameClient.Core;

namespace GameClient.Managers
{
    [Serializable]
    public class TimePackage
    {
        public string packageId;
        public long expirationTimestampMs; // Thời điểm hết hạn (Unix time)
    }

    public class PlayerProfileManager : Singleton<PlayerProfileManager>
    {
        public List<TimePackage> ActivePackages = new List<TimePackage>();

        protected override void Awake()
        {
            base.Awake();
        }

        public bool HasActivePackage(string packageId)
        {
            long currentUnixTime = TimestampManager.Instance.GetCurrentServerTimeMs();

            for (int i = 0; i < ActivePackages.Count; i++)
            {
                if (ActivePackages[i].packageId == packageId)
                {
                    if (ActivePackages[i].expirationTimestampMs >= currentUnixTime)
                    {
                        return true; // Gói còn hiệu lực
                    }
                    else
                    {
                        Debug.Log($"[Profile] Gói {packageId} đã hết hạn!");
                        ActivePackages.RemoveAt(i);
                        return false;
                    }
                }
            }

            return false;
        }

        public void PurchasePackage(string packageId, int daysDuration)
        {
            long durationMs = (long)daysDuration * 24 * 60 * 60 * 1000;
            long currentUnixTime = TimestampManager.Instance.GetCurrentServerTimeMs();

            var existingPackage = ActivePackages.Find(p => p.packageId == packageId);

            if (existingPackage != null)
            {
                if (existingPackage.expirationTimestampMs > currentUnixTime)
                {
                    existingPackage.expirationTimestampMs += durationMs;
                }
                else
                {
                    existingPackage.expirationTimestampMs = currentUnixTime + durationMs;
                }
            }
            else
            {
                ActivePackages.Add(new TimePackage
                {
                    packageId = packageId,
                    expirationTimestampMs = currentUnixTime + durationMs
                });
            }

            Debug.Log($"[Profile] Đã mua thành công gói {packageId} ({daysDuration} ngày). " +
                      $"Thời hạn mới: {DateTimeOffset.FromUnixTimeMilliseconds(ActivePackages.Find(p => p.packageId == packageId).expirationTimestampMs).ToLocalTime()}");
        }
    }
}
