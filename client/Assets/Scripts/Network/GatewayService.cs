using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using UnityEngine;
using GameClient.Network.Pb;

namespace GameClient.Network
{
    public static class GatewayClientApi
    {
        private static Pb.GatewayService.GatewayServiceClient Client => NetworkManager.Instance.GatewayClient;

        private static void AssertConnected()
        {
            if (Client == null)
                throw new InvalidOperationException("GatewayClient chưa khởi tạo.");
        }

        public static async Task<CreatePlayerResponse> CreatePlayerAsync(string nickname)
        {
            AssertConnected();
            return await Client.CreatePlayerAsync(new CreatePlayerRequest { Nickname = nickname }, NetworkManager.DefaultCallOptions());
        }

        public static async Task<GetPlayerProfileResponse> GetPlayerProfileAsync()
        {
            AssertConnected();
            return await Client.GetPlayerProfileAsync(new GetPlayerProfileRequest(), NetworkManager.DefaultCallOptions());
        }

        public static async Task<GetBaseResponse> GetBaseAsync()
        {
            AssertConnected();
            return await Client.GetBaseAsync(new GetBaseRequest(), NetworkManager.DefaultCallOptions());
        }

        public static async Task<UpgradeBuildingResponse> UpgradeBuildingAsync(string buildingCode)
        {
            AssertConnected();
            return await Client.UpgradeBuildingAsync(new UpgradeBuildingRequest { BuildingCode = buildingCode }, NetworkManager.DefaultCallOptions());
        }

        public static async Task<CollectResourcesResponse> CollectResourcesAsync(string buildingCode)
        {
            AssertConnected();
            return await Client.CollectResourcesAsync(new CollectResourcesRequest { BuildingCode = buildingCode }, NetworkManager.DefaultCallOptions());
        }

        public static async Task<GetHeroesResponse> GetHeroesAsync()
        {
            AssertConnected();
            return await Client.GetHeroesAsync(new GetHeroesRequest(), NetworkManager.DefaultCallOptions());
        }

        public static async Task<SetFormationResponse> SetFormationAsync(List<(int position, long playerHeroId)> slots)
        {
            AssertConnected();
            var req = new SetFormationRequest();
            foreach (var (pos, heroId) in slots)
                req.Slots.Add(new FormationSlot { Position = pos, PlayerHeroId = heroId });
            return await Client.SetFormationAsync(req, NetworkManager.DefaultCallOptions());
        }

        public static async Task<GetGachaBannersResponse> GetGachaBannersAsync()
        {
            AssertConnected();
            return await Client.GetGachaBannersAsync(new GetGachaBannersRequest(), NetworkManager.DefaultCallOptions());
        }

        public static async Task<DoGachaResponse> DoGachaAsync(int bannerId, int count)
        {
            AssertConnected();
            return await Client.DoGachaAsync(new DoGachaRequest { BannerId = bannerId, Count = count }, NetworkManager.DefaultCallOptions());
        }
    }
}
