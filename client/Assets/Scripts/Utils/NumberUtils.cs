using System;

namespace GameClient.Utils
{
    public static class NumberUtils
    {
        /// <summary>
        /// Rút gọn số lớn thành định dạng k, m, b... (Ví dụ: 1000 -> 1.00k)
        /// </summary>
        public static string FormatNumber(double value)
        {
            if (value < 1000)
            {
                return value.ToString("N0");
            }

            string[] suffixes = { "", "k", "m", "b", "t" };
            int i = 0;
            while (value >= 1000 && i < suffixes.Length - 1)
            {
                value /= 1000;
                i++;
            }

            // Round down to 1 decimal place to prevent rounding up (e.g. 600.05k -> 600.0k instead of 600.1k)
            value = Math.Floor(value * 10) / 10;
            
            // Trả về dạng 1.0k, 12.5m...
            return value.ToString("0.0") + suffixes[i];
        }
    }
}
