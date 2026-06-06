using System.Threading.Tasks;

namespace GameClient.Minigames
{
    public interface IMinigame
    {
        string MinigameId { get; }
        
        Task<bool> PlayAsync();
    }
}
