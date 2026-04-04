using _Game.Scripts.Configs;
using _Game.Scripts.Core;

namespace _Game.Scripts.Services
{
    public class CursorService : IService
    {
        private readonly CursorController _controller;

        public CursorService(CursorController controller) => _controller = controller;

        public void SetCursor(CursorConfig config) => _controller.Apply(config);
    }
}
