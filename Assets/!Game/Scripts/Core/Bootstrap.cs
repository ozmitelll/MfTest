using _Game.Scripts.Configs;
using _Game.Scripts.Services;
using UnityEngine;

namespace _Game.Scripts.Core
{
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private ServiceLocator serviceLocatorPrefab;
        [SerializeField] private GameConfig _gameConfig;

        void Awake()
        {
            var serviceLocator = Instantiate(serviceLocatorPrefab);

            var sessionService = new SessionService();
            sessionService.SetGameConfig(_gameConfig);
            serviceLocator.Register(sessionService);

            var sceneService = new SceneService(serviceLocator);
            serviceLocator.Register(sceneService);
            
            sceneService.LoadScene(SceneNames.Menu, 0);
        }
    }
}