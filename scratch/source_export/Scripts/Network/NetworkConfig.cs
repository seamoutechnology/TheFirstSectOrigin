namespace GameClient.Network
{
    public static class NetworkConfig
    {
        public const string BASE_URL = GameClient.Core.GameConstants.Network.API_BASE_URL;
        
        public const string API_LOGIN = "/api/v1/auth/login";
        public const string API_REGISTER = "/api/v1/auth/register";
        public const string API_GET_PLAYER = "/api/v1/player/info";
        public const string API_GET_SQUAD = "/api/v1/player/squad";
        
        public const float TIMEOUT = 15.0f;
    }
}
