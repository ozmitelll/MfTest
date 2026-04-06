using System.Collections.Generic;
using _Game.Scripts.Configs;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Systems.Combat;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Scripts.UI
{
    [RequireComponent(typeof(EnemyBarsLayer))]
    public class EnemyDamageNumbersManager : MonoBehaviour
    {
        [SerializeField] private EnemyDamageNumbersConfig _config;
        [SerializeField] private EnemyBarsLayer _layer;

        private sealed class ActiveDamageNumber
        {
            public Label Label;
            public Transform Anchor;
            public Vector3 FallbackWorldPosition;
            public Vector2 SpawnOffset;
            public Vector2 DriftOffset;
            public float Lifetime;
            public float Age;
        }

        private readonly List<ActiveDamageNumber> _activeNumbers = new();

        private Camera _camera;

        private void Awake()
        {
            _layer ??= GetComponent<EnemyBarsLayer>();
        }

        private void Start()
        {
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnEntityDamagedEvent>(OnEntityDamaged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnEntityDamagedEvent>(OnEntityDamaged);
            ClearActiveNumbers();
        }

        private void Update()
        {
            if (_activeNumbers.Count == 0)
                return;

            if (_camera == null)
                _camera = Camera.main;

            UpdateDamageNumbers();
        }

        private void OnEntityDamaged(OnEntityDamagedEvent damageEvent)
        {
            if (_config == null || damageEvent.Target == null || damageEvent.Damage <= 0f)
                return;

            if (damageEvent.Target.GetComponent<_Game.Scripts.Gameplay.Entities.Enemy.Enemy>() == null)
                return;

            VisualElement root = GetRoot();
            if (root == null)
                return;

            EnemyDamageNumbersConfig.DamageTypeVisualSettings settings = _config.GetSettings(damageEvent.DamageType);
            Vector2 spawnOffset = new(
                Random.Range(-_config.HorizontalSpawnRadius, _config.HorizontalSpawnRadius),
                Random.Range(-_config.VerticalSpawnRadius, _config.VerticalSpawnRadius));

            Vector2 driftOffset = new(
                Random.Range(-_config.HorizontalDrift, _config.HorizontalDrift),
                -_config.RiseDistance);

            Label label = new()
            {
                text = Mathf.Max(1, Mathf.RoundToInt(damageEvent.Damage)).ToString()
            };

            label.style.position = Position.Absolute;
            label.style.fontSize = settings.FontSize;
            label.style.color = ResolveDamageColor(damageEvent, settings);
            label.style.opacity = 1f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.pickingMode = PickingMode.Ignore;

            root.Add(label);

            ActiveDamageNumber damageNumber = new()
            {
                Label = label,
                Anchor = damageEvent.Target.transform,
                FallbackWorldPosition = damageEvent.Target.transform.position + _config.WorldOffset,
                SpawnOffset = spawnOffset,
                DriftOffset = driftOffset,
                Lifetime = _config.Lifetime,
                Age = 0f
            };

            _activeNumbers.Add(damageNumber);
            UpdateDamageNumber(damageNumber, 0f);
        }

        private void UpdateDamageNumbers()
        {
            for (int index = _activeNumbers.Count - 1; index >= 0; index--)
            {
                ActiveDamageNumber damageNumber = _activeNumbers[index];
                if (damageNumber.Label == null)
                {
                    _activeNumbers.RemoveAt(index);
                    continue;
                }

                damageNumber.Age += Time.deltaTime;
                float progress = damageNumber.Lifetime > 0f
                    ? Mathf.Clamp01(damageNumber.Age / damageNumber.Lifetime)
                    : 1f;

                UpdateDamageNumber(damageNumber, progress);

                if (progress < 1f)
                    continue;

                damageNumber.Label.RemoveFromHierarchy();
                _activeNumbers.RemoveAt(index);
            }
        }

        private void UpdateDamageNumber(ActiveDamageNumber damageNumber, float progress)
        {
            if (damageNumber.Label == null)
                return;

            if (!TryGetScreenPosition(damageNumber, out Vector2 screenPosition))
            {
                damageNumber.Label.style.display = DisplayStyle.None;
                return;
            }

            Vector2 animatedOffset = damageNumber.SpawnOffset + damageNumber.DriftOffset * progress;
            Vector2 finalPosition = screenPosition + animatedOffset;

            damageNumber.Label.style.display = DisplayStyle.Flex;
            damageNumber.Label.style.left = finalPosition.x;
            damageNumber.Label.style.top = finalPosition.y;
            damageNumber.Label.style.opacity = 1f - progress;
        }

        private bool TryGetScreenPosition(ActiveDamageNumber damageNumber, out Vector2 screenPosition)
        {
            screenPosition = default;

            if (_camera == null)
                return false;

            Vector3 worldAnchorPosition = damageNumber.Anchor != null
                ? damageNumber.Anchor.position + _config.WorldOffset
                : damageNumber.FallbackWorldPosition;

            Vector3 rawScreenPosition = _camera.WorldToScreenPoint(worldAnchorPosition);
            if (rawScreenPosition.z < 0f)
                return false;

            screenPosition = new Vector2(rawScreenPosition.x, Screen.height - rawScreenPosition.y);
            return true;
        }

        private VisualElement GetRoot()
        {
            if (_layer == null)
                _layer = GetComponent<EnemyBarsLayer>();

            return _layer != null ? _layer.Root : null;
        }

        private Color ResolveDamageColor(
            OnEntityDamagedEvent damageEvent,
            EnemyDamageNumbersConfig.DamageTypeVisualSettings settings)
        {
            if (damageEvent.IsStatusDamage &&
                damageEvent.StatusEffect != null &&
                _config.TryGetStatusColor(damageEvent.StatusEffect, out Color statusColor))
                return statusColor;

            return settings.ResolveColor(damageEvent.IsStatusDamage);
        }

        private void ClearActiveNumbers()
        {
            for (int index = 0; index < _activeNumbers.Count; index++)
                _activeNumbers[index].Label?.RemoveFromHierarchy();

            _activeNumbers.Clear();
        }
    }
}