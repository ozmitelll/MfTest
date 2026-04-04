using UnityEngine;

namespace _Game.Scripts.Gameplay.Systems.StatusEffects
{
    [CreateAssetMenu(fileName = "StatusEffect", menuName = "Modfall/Status Effects/Definition")]
    public class StatusEffectDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea] private string _description;

        [Header("Runtime")]
        [SerializeField] [Min(0.05f)] private float _defaultDuration = 1f;
        [SerializeField] private StatusEffectStackingMode _stackingMode = StatusEffectStackingMode.RefreshDuration;
        [SerializeField] [Min(1)] private int _maxStacks = 1;
        [SerializeField] private bool _restartTickOnReapply = true;

        [Header("Ticking")]
        [SerializeField] private StatusEffectTickMode _tickMode = StatusEffectTickMode.None;
        [SerializeField] [Min(0.05f)] private float _fixedTickInterval = 1f;

        [Header("Damage Over Time")]
        [SerializeField] [Min(0f)] private float _flatDamagePerSecond;
        [SerializeField] [Min(0f)] private float _sourceDamagePercentPerSecond;
        [SerializeField] [Min(0f)] private float _targetCurrentHealthPercentPerSecond;

        [Header("Control")]
        [SerializeField] private bool _blocksMovement;
        [SerializeField] private bool _blocksSkills;
        [SerializeField] private bool _ignoreNonLethalDamage;

        [Header("Runtime Modifiers")]
        [SerializeField] private float _moveSpeedPercentPerStack;
        [SerializeField] private StatusModifierStackingMode _moveSpeedStackingMode = StatusModifierStackingMode.Linear;
        [SerializeField] private float _incomingDamageTakenPercentPerStack;

        public string Id => string.IsNullOrWhiteSpace(_id) ? name : _id;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public string Description => _description;
        public float DefaultDuration => _defaultDuration;
        public StatusEffectStackingMode StackingMode => _stackingMode;
        public int MaxStacks => _maxStacks;
        public bool RestartTickOnReapply => _restartTickOnReapply;
        public StatusEffectTickMode TickMode => _tickMode;
        public float FixedTickInterval => _fixedTickInterval;
        public float FlatDamagePerSecond => _flatDamagePerSecond;
        public float SourceDamagePercentPerSecond => _sourceDamagePercentPerSecond;
        public float TargetCurrentHealthPercentPerSecond => _targetCurrentHealthPercentPerSecond;
        public bool BlocksMovement => _blocksMovement;
        public bool BlocksSkills => _blocksSkills;
        public bool IgnoreNonLethalDamage => _ignoreNonLethalDamage;
        public float MoveSpeedPercentPerStack => _moveSpeedPercentPerStack;
        public StatusModifierStackingMode MoveSpeedStackingMode => _moveSpeedStackingMode;
        public float IncomingDamageTakenPercentPerStack => _incomingDamageTakenPercentPerStack;
    }
}
