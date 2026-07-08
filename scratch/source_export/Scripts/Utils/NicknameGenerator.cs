using UnityEngine;
using System.Text;

namespace GameClient.Utils
{
    public static class NicknameGenerator
    {
        private static readonly string[] FirstNames = { "Tieu", "Lam", "Han", "So", "Mo", "Lanh", "Diep", "Duong", "Tran", "Nguyen" };
        private static readonly string[] MiddleNames = { "Vo", "Kiem", "Thien", "Phong", "Chi", "Van", "Tieu", "Dai", "Doc" };
        private static readonly string[] LastNames = { "Viem", "Dong", "Lap", "Phong", "Ngon", "Huyet", "Pham", "Long", "Than", "Ton" };

        private static readonly string[] Prefixes = { "Pro", "Vip", "Vua", "Than", "Sieu", "BaChu", "Lord", "Dark", "Chuan" };
        private static readonly string[] Suffixes = { "Solo", "99", "102", "PvP", "TuTien", "Origin", "Sect", "No1" };

        public static string Generate(int style = 0, bool noDiacritics = true, bool noSpaces = true, bool addNumbers = true)
        {
            string baseName = "";

            if (style == 0) // Kiểu Tiên Hiệp
            {
                string first = FirstNames[Random.Range(0, FirstNames.Length)];
                string middle = Random.Range(0, 10) > 3 ? MiddleNames[Random.Range(0, MiddleNames.Length)] + " " : ""; // 60% có tên đệm
                string last = LastNames[Random.Range(0, LastNames.Length)];
                
                baseName = $"{first} {middle}{last}".Trim();
            }
            else // Kiểu Gamer / Slang
            {
                string prefix = Prefixes[Random.Range(0, Prefixes.Length)];
                string mainName = LastNames[Random.Range(0, LastNames.Length)];
                string suffix = Random.Range(0, 2) == 0 ? Suffixes[Random.Range(0, Suffixes.Length)] : "";

                if (string.IsNullOrEmpty(suffix))
                {
                    baseName = $"{prefix} {mainName}".Trim();
                }
                else
                {
                    baseName = $"{prefix} {mainName} {suffix}".Trim();
                }
            }

            if (noDiacritics)
            {
                baseName = RemoveDiacritics(baseName);
            }

            if (noSpaces)
            {
                baseName = baseName.Replace(" ", "");
            }

            if (addNumbers)
            {
                int randNum = Random.Range(0, 3) switch
                {
                    0 => Random.Range(1, 100),       // 1 - 99
                    1 => Random.Range(1990, 2027),   // Năm sinh
                    _ => Random.Range(1000, 9999)    // Số 4 chữ số
                };
                baseName += randNum.ToString();
            }

            return baseName;
        }

        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string[] arr1 = new string[] { 
                "á", "à", "ả", "ã", "ạ", "â", "ấ", "ầ", "ẩ", "ẫ", "ậ", "ă", "ắ", "ằ", "ẳ", "ẵ", "ặ",
                "đ",
                "é","è","ẻ","ẽ","ẹ","ê","ế","ề","ể","ễ","ệ",
                "í","ì","ỉ","ĩ","ị",
                "ó","ò","ỏ","õ","ọ","ô","ố","ồ","ổ","ỗ","ộ","ơ","ớ","ờ","ở","ỡ","ợ",
                "ú","ù","ủ","ũ","ụ","ư","ứ","ừ","ử","ữ","ự",
                "ý","ỳ","ỷ","ỹ","ỵ"
            };
            string[] arr2 = new string[] { 
                "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a",
                "d",
                "e","e","e","e","e","e","e","e","e","e","e",
                "i","i","i","i","i",
                "o","o","o","o","o","o","o","o","o","o","o","o","o","o","o","o","o",
                "u","u","u","u","u","u","u","u","u","u","u",
                "y","y","y","y","y"
            };

            for (int i = 0; i < arr1.Length; i++)
            {
                text = text.Replace(arr1[i], arr2[i]);
                text = text.Replace(arr1[i].ToUpper(), arr2[i].ToUpper());
            }
            return text;
        }
    }
}
