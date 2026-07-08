using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace GameClient.Utils
{
    public static class CryptoUtils
    {
        private static byte[] GetKey()
        {
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = "fallback_device_id_for_editor";
            }
            
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(deviceId));
            }
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";

            byte[] key = GetKey();
            byte[] iv = new byte[16];

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(iv);
                }
                aes.IV = iv;

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length);

                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherTextBase64)
        {
            if (string.IsNullOrEmpty(cipherTextBase64)) return "";

            try
            {
                byte[] cipherBytesWithIv = Convert.FromBase64String(cipherTextBase64);
                byte[] key = GetKey();

                byte[] iv = new byte[16];
                Array.Copy(cipherBytesWithIv, 0, iv, 0, iv.Length);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytesWithIv, iv.Length, cipherBytesWithIv.Length - iv.Length);
                            cs.FlushFinalBlock();
                        }

                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CryptoUtils] Lỗi giải mã dữ liệu: {ex.Message}");
                return "";
            }
        }
    }
}
