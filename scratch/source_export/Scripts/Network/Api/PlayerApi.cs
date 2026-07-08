using System.Threading.Tasks;
using GameClient.Network.Pb;

namespace GameClient.Network.Api
{
    public static class PlayerApi
    {
        private static Pb.GatewayService.GatewayServiceClient Client => NetworkManager.Instance.GatewayClient;

        public static async Task<CreatePlayerResponse> CreatePlayerAsync(string nickname)
        {
            return await Client.CreatePlayerAsync(new CreatePlayerRequest { Nickname = nickname }, NetworkManager.DefaultCallOptions());
        }

        public static async Task<GetPlayerProfileResponse> GetPlayerProfileAsync()
        {
            return await Client.GetPlayerProfileAsync(new GetPlayerProfileRequest(), NetworkManager.DefaultCallOptions());
        }
    }
}
