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

        public static async Task<UpgradeBuildingResponse> UpgradeBuildingAsync(long instanceId)
        {
            return await Client.UpgradeBuildingAsync(new UpgradeBuildingRequest { InstanceId = instanceId }, NetworkManager.DefaultCallOptions());
        }

        public static async Task<CollectResourcesResponse> CollectResourcesAsync(long instanceId)
        {
            return await Client.CollectResourcesAsync(new CollectResourcesRequest { InstanceId = instanceId }, NetworkManager.DefaultCallOptions());
        }

        public static async Task<SpeedUpBuildingResponse> SpeedUpBuildingAsync(long instanceId)
        {
            return await Client.SpeedUpBuildingAsync(new SpeedUpBuildingRequest { InstanceId = instanceId }, NetworkManager.DefaultCallOptions());
        }

        public static async Task<Inventory> GetInventoryAsync()
        {
            return await Client.GetInventoryAsync(new GetProfileRequest(), NetworkManager.DefaultCallOptions());
        }

        public static async Task<UseItemResponse> UseItemAsync(long itemId, int quantity)
        {
            return await Client.UseItemAsync(new UseItemRequest { ItemId = itemId, Quantity = quantity }, NetworkManager.DefaultCallOptions());
        }

        public static async Task<GetCompletedStagesResponse> GetCompletedStagesAsync()
        {
            return await Client.GetCompletedStagesAsync(new GetCompletedStagesRequest(), NetworkManager.DefaultCallOptions());
        }

        public static async Task<GetLeaderboardResponse> GetLeaderboardAsync(string type)
        {
            return await Client.GetLeaderboardAsync(new GetLeaderboardRequest { Type = type }, NetworkManager.DefaultCallOptions());
        }

        public static async Task<GetShopResponse> GetShopAsync(string shopType)
        {
            return await Client.GetShopAsync(new GetShopRequest { ShopType = shopType }, NetworkManager.DefaultCallOptions());
        }

        public static async Task<RefreshShopResponse> RefreshShopAsync(string shopType)
        {
            return await Client.RefreshShopAsync(new RefreshShopRequest { ShopType = shopType }, NetworkManager.DefaultCallOptions());
        }

        public static async Task<BuyShopItemResponse> BuyShopItemAsync(long instanceId, int quantity)
        {
            return await Client.BuyShopItemAsync(new BuyShopItemRequest { InstanceId = instanceId, Quantity = quantity }, NetworkManager.DefaultCallOptions());
        }
    }
}
