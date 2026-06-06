using System.Threading.Tasks;
using GameClient.Network.Pb;

namespace GameClient.Network.Api
{
    public static class GachaApi
    {
        private static Pb.GatewayService.GatewayServiceClient Client => NetworkManager.Instance.GatewayClient;

        public static async Task<GetGachaBannersResponse> GetGachaBannersAsync()
        {
            return await Client.GetGachaBannersAsync(new GetGachaBannersRequest(), NetworkManager.DefaultCallOptions());
        }

        public static async Task<DoGachaResponse> DoGachaAsync(int bannerId, int count)
        {
            return await Client.DoGachaAsync(new DoGachaRequest { BannerId = bannerId, Count = count }, NetworkManager.DefaultCallOptions());
        }
    }
}
