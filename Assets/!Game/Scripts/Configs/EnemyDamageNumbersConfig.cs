using System;
using _Game.Scripts.Gameplay.Systems.Combat;
using _Game.Scripts.Gameplay.Systems.StatusEffects;
using UnityEngine;

namespace _Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "EnemyDamageNumbersConfig", menuName = "Modfall/Configs/Enemy Damage Numbers Config")]
    public class EnemyDamageNumbersConfig : ScriptableObject
    {
        [Serializable]
        public struct DamageTypeVisualSettings
        {
            public Color DirectColor;
            public Color StatusColor;
            [Min(8)] public int FontSize;

            public Color ResolveColor(bool isStatusDamage) => isStatusDamage ? StatusColor : DirectColor;
        }

        [Serializable]
        public struct StatusEffectVisualSettings
        {
            public StatusEffectDefinition StatusEffect;
            public Color Color;
        }

        [Header("Placement")]
        public Vector3 WorldOffset = new(0f, 2.2f, 0f);
        [Min(0f)] public float HorizontalSpawnRadius = 30f;
        [Min(0f)] public float VerticalSpawnRadius = 16f;

        [Header("Animation")]
        [Min(0.1f)] public float Lifetime = 0.7f;
        [Min(0f)] public float RiseDistance = 42f;
        [Min(0f)] public float HorizontalDrift = 10f;

        [Header("Per Type")]
        public DamageTypeVisualSettings Pure = new()
        {
            DirectColor = new Color(0.95f, 0.95f, 0.95f, 1f),
            StatusColor = new Color(0.72f, 0.72f, 0.72f, 1f),
            FontSize = 20
        };

        public DamageTypeVisualSettings Magical = new()
        {
            DirectColor = new Color(0.45f, 0.82f, 1f, 1f),
            StatusColor = new Color(0.3f, 0.95f, 1f, 1f),
            FontSize = 20
        };

        public DamageTypeVisualSettings Elemental = new()
        {
            DirectColor = new Color(1f, 0.64f, 0.25f, 1f),
            StatusColor = new Color(1f, 0.82f, 0.35f, 1f),
            FontSize = 20
        };

        [Header("Status Overrides")]
        public StatusEffectVisualSettings[] StatusEffects = Array.Empty<StatusEffectVisualSettings>();

        public DamageTypeVisualSettings GetSettings(DamageType damageType) => damageType switch
        {
            DamageType.Magical => Magical,
            DamageType.Elemental => Elemental,
            _ => Pure
        };

        public bool TryGetStatusColor(StatusEffectDefinition statusEffect, out Color color)
        {
            if (StatusEffects != null)
            {
                for (int index = 0; index < StatusEffects.Length; index++)
                {
                    if (StatusEffects[index].StatusEffect != statusEffect)
                        continue;

                    color = StatusEffects[index].Color;
                    return true;
                }
            }

            color = default;
            return false;
        }
    }
}