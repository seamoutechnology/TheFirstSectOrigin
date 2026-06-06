using System.Collections.Generic;
using System.Threading.Tasks;
using GameClient.Network.Pb;

namespace GameClient.Network.Api
{
    public static class DiscipleApi
    {
        private static Pb.GatewayService.GatewayServiceClient Client => NetworkManager.Instance.GatewayClient;

        public static async Task<GetHeroesResponse> GetHeroesAsync()
        {
            return await Client.GetHeroesAsync(new GetHeroesRequest(), NetworkManager.DefaultCallOptions());
        }

        public static async Task<SetFormationResponse> SetFormationAsync(List<(int position, long playerHeroId)> slots)
        {
            var req = new SetFormationRequest();
            foreach (var (pos, heroId) in slots)
                req.Slots.Add(new FormationSlot { Position = pos, PlayerHeroId = heroId });
            return await Client.SetFormationAsync(req, NetworkManager.DefaultCallOptions());
        }

        public static async Task<LevelUpHeroResponse> LevelUpHeroAsync(long heroId)
        {
            var req = new LevelUpHeroRequest { HeroId = heroId };
            return await Client.LevelUpHeroAsync(req, NetworkManager.DefaultCallOptions());
        }
        
    }
}
