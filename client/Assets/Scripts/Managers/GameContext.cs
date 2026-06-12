using UnityEngine;

namespace GameClient.Managers
{
    public static class GameContext
    {
        public static string CurrentServerHost { get; set; }
        public static int CurrentServerPort { get; set; }
        public static string CurrentServerName { get; set; }
        public static int CurrentServerId { get; set; }
        
        public static bool HasCharacter { get; set; } = false;

        public static System.Action OnServerDataSynced;

        public static int SectReputation { get; set; } = 0; // Danh tiếng tông môn
        public static int SectAlignment { get; set; } = 0; // Thiện/Ác tông môn (âm là Ác, dương là Thiện)
        public static int SectLevel { get; set; } = 1; // Cấp độ tông môn
        public static int AdditionalBuildingCapacity { get; set; } = 0; // Sức chứa đệ tử từ các công trình phụ
        public static int AdditionalWaitingCapacity { get; set; } = 0; // Sức chứa hàng chờ từ công trình phụ
        public static int MaxDiscipleCapacity => SectLevel * 10 + AdditionalBuildingCapacity; 
        public static int MaxWaitingQueue => SectLevel * 5 + AdditionalWaitingCapacity; // Hàng chờ tăng theo cấp độ và nhà
        
        public static bool IsEvil => SectAlignment < -50;
        public static bool IsGood => SectAlignment > 50;
        public static bool IsNeutral => SectAlignment >= -50 && SectAlignment <= 50;
        
        public static void Clear()
        {
            CurrentServerHost = string.Empty;
            CurrentServerPort = 0;
            CurrentServerName = string.Empty;
            CurrentServerId = 1;
            HasCharacter = false;
            SectReputation = 0;
            SectAlignment = 0;
            SectLevel = 1;
        }
    }
}
