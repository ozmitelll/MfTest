using _Game.Scripts.Core;
using _Game.Scripts.Services;
using UnityEngine;
using EnemyEntity = _Game.Scripts.Gameplay.Entities.Enemy.Enemy;

namespace _Game.Scripts.Gameplay.Entities.Player.Systems
{
    public class PlayerWallet : MonoBehaviour
    {
        private Player _player;
        private SessionService _sessionService;
        private bool _initialized;
        private bool _subscribed;

        public int Coins { get; private set; }

        public void Initialize(Player player)
        {
            _player = player;
            Coins = 0;
            _initialized = true;

            if (isActiveAndEnabled)
                SubscribeToEvents();
        }

        private void OnEnable()
        {
            if (!_initialized)
                return;

            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        public bool CanAfford(int amount) => amount <= 0 || Coins >= amount;

        public void AddCoins(int amount)
        {
            if (amount <= 0)
                return;

            Coins += amount;
            PublishCoinsChanged();
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0)
                return true;

            if (Coins < amount)
                return false;

            Coins -= amount;
            PublishCoinsChanged();
            return true;
        }

        private void OnEnemyDied(OnEnemyDiedEvent evt)
        {
            if (!_initialized || _player == null || evt.Enemy == null)
                return;

            ResolveSessionService();
            int reward = _sessionService != null
                ? _sessionService.GetEnemyCoinReward(evt.Enemy)
                : GetFallbackReward(evt.Enemy);

            AddCoins(reward);
        }

        private void ResolveSessionService()
        {
            if (_sessionService == null && ServiceLocator.Instance != null && ServiceLocator.Instance.Has<SessionService>())
                _sessionService = ServiceLocator.Instance.Get<SessionService>();
        }

        private static int GetFallbackReward(EnemyEntity enemy)
        {
            if (enemy == null || enemy.Config == null)
                return 0;

            int minReward = Mathf.Max(0, enemy.Config.CoinDropMin);
            int maxReward = Mathf.Max(minReward, enemy.Config.CoinDropMax);
            return Random.Range(minReward, maxReward + 1);
        }

        private void SubscribeToEvents()
        {
            if (_subscribed)
                return;

            EventBus.Subscribe<OnEnemyDiedEvent>(OnEnemyDied);
            _subscribed = true;
            PublishCoinsChanged();
        }

        private void UnsubscribeFromEvents()
        {
            if (!_subscribed)
                return;

            EventBus.Unsubscribe<OnEnemyDiedEvent>(OnEnemyDied);
            _subscribed = false;
        }

        private void PublishCoinsChanged() => EventBus.Publish(new OnCoinsChangedEvent { Coins = Coins });
    }
}