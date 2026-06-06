using System.Threading.Tasks;
using GameClient.Network.Pb;

namespace GameClient.Network.Api
{
    public static class SectBuildingApi
    {
        private static Pb.GatewayService.GatewayServiceClient Client => NetworkManager.Instance.GatewayClient;

        public static async Task<GetBaseResponse> GetBaseAsync()
        {
            return await Client.GetBaseAsync(new GetBaseRequest(), NetworkManager.DefaultCallOptions());
        }

        public static async Task<UpgradeBuildingResponse> UpgradeBuildingAsync(string buildingCode)
        {
            return await Client.UpgradeBuildingAsync(new UpgradeBuildingRequest { BuildingCode = buildingCode }, NetworkManager.DefaultCallOptions());
        }

        public static async Task<CollectResourcesResponse> CollectResourcesAsync(string buildingCode)
        {
            return await Client.CollectResourcesAsync(new CollectResourcesRequest { BuildingCode = buildingCode }, NetworkManager.DefaultCallOptions());
        }
    }
}
