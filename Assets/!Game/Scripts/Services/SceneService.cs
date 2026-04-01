using System.Collections;
using _Game.Scripts.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Game.Scripts.Services
{
    public static class SceneNames
    {
        public const string Menu = "Menu";
        public const string Game = "Game";
    }
    public class SceneService : IService
    {
        private MonoBehaviour _runner;
        
        public SceneService(MonoBehaviour runner)
        {
            _runner = runner;
        }

        public void LoadScene(string sceneName, float delay)
        {
            _runner.StartCoroutine(LoadRoutine(sceneName, delay));
        }

        private IEnumerator LoadRoutine(string sceneName, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(sceneName);
        }
    }
    
}
