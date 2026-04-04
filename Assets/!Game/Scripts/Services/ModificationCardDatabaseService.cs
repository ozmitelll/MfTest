using _Game.Scripts.Configs;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Systems.Modifications;

namespace _Game.Scripts.Services
{
    public class ModificationCardDatabaseService : IService
    {
        private readonly ModificationCardDatabase _database;

        public ModificationCardDatabaseService(ModificationCardDatabase database)
        {
            _database = database;
        }

        public bool TryGetRandomCard(out ModificationCardDefinition card)
        {
            card = null;
            return _database != null && _database.TryGetRandomCard(out card);
        }
    }
}
