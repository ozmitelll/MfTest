using System;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities.Player;
using _Game.Scripts.Gameplay.Interactables;

namespace _Game.Scripts.Services
{
    public class ModificationScreenService : IService
    {
        public event Action<ModificationStationNpc, Player> OpenRequested;
        public event Action Closed;

        public bool IsOpen { get; private set; }
        public Player CurrentPlayer { get; private set; }
        public ModificationStationNpc CurrentStation { get; private set; }

        public void Open(ModificationStationNpc station, Player player)
        {
            if (station == null || player == null)
                return;

            CurrentStation = station;
            CurrentPlayer = player;
            IsOpen = true;
            OpenRequested?.Invoke(station, player);
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            CurrentStation = null;
            CurrentPlayer = null;
            Closed?.Invoke();
        }
    }
}
