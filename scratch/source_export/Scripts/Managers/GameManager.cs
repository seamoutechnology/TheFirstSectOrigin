using System.Collections.Generic;
using GameClient.Network.Pb;
using UnityEngine;

namespace GameClient
{
    public partial class GameManager : Singleton<GameManager>
    {
        public PlayerProfile CurrentPlayer    { get; private set; }
        public List<Building> PlayerBuildings { get; private set; } = new();
        public List<Hero>     PlayerHeroes    { get; private set; } = new();
        public List<FormationSlot> Formation  { get; private set; } = new();
        public HashSet<string> CompletedStages { get; private set; } = new();

        public System.Action<PlayerProfile>   OnPlayerUpdated;
        public System.Action<List<Building>>  OnBaseUpdated;
        public System.Action<List<Hero>>      OnHeroesUpdated;
        public System.Action                  OnCompletedStagesUpdated;


        public void SetPlayer(PlayerProfile player)
        {
            CurrentPlayer = player;
            OnPlayerUpdated?.Invoke(player);
        }

        public void SetCompletedStages(IEnumerable<string> stageIds)
        {
            CompletedStages = new HashSet<string>(stageIds);
            OnCompletedStagesUpdated?.Invoke();
        }

        public void SetBuildings(IEnumerable<Building> buildings)
        {
            PlayerBuildings = new List<Building>(buildings);
            OnBaseUpdated?.Invoke(PlayerBuildings);
        }

        public void SetHeroes(IEnumerable<Hero> heroes)
        {
            PlayerHeroes = new List<Hero>(heroes);
            OnHeroesUpdated?.Invoke(PlayerHeroes);
        }

        public void SetFormation(IEnumerable<FormationSlot> formation)
        {
            Formation = new List<FormationSlot>(formation);
        }

        public void ClearData()
        {
            CurrentPlayer = null;
            PlayerBuildings.Clear();
            PlayerHeroes.Clear();
            Formation.Clear();
            PlayerPrefs.DeleteKey(GameClient.Core.GameConstants.PlayerPrefsKeys.TOKEN);
            PlayerPrefs.DeleteKey("user_id");
        }
    }
}
