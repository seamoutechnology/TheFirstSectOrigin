namespace GameClient.Gameplay.Heroes
{
    public static class CultivationUtility
    {
        private static readonly string[] Realms = new string[]
        {
            "Thường Nhân",
            "Ngưng Khí",
            "Trúc Cơ",
            "Kết Đan",
            "Nguyên Anh",
            "Hóa Thần",
            "Luyện Hư",
            "Hợp Thể",
            "Đại Thừa",
            "Độ Kiếp"
        };

        private static readonly string[] SubRealms = new string[]
        {
            "Hạ Kỳ",
            "Trung Kỳ",
            "Thượng Kỳ"
        };

        public static string GetCultivationRealm(int level)
        {
            if (level < 10)
            {
                return $"Thường Nhân cấp {level}";
            }

            
            int realmIndex = (level - 10) / 30 + 1; 
            if (realmIndex >= Realms.Length)
            {
                return "Phi Thăng Tiên Giới"; // Max cấp
            }

            int levelInRealm = (level - 10) % 30;
            int subRealmIndex = levelInRealm / 10;
            
            int star = levelInRealm % 10;

            string realmName = Realms[realmIndex];
            string subRealmName = SubRealms[subRealmIndex];

            return $"{realmName} {subRealmName} {star} Sao";
        }
    }
}
