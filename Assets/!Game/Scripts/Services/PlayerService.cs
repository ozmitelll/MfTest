using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities.Player;

namespace _Game.Scripts.Services
{
    public class PlayerService : IService
    {
        public Player Player { get; private set; }

        public void SetPlayer(Player player)
        {
            Player = player;
        }
    }
}
