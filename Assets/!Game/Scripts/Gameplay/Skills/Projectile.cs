using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Systems.Combat;
using _Game.Scripts.Gameplay.Systems.StatusEffects;
using _Game.Scripts.Services;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Skills
{
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _lifetime = 5f;

        private Vector3 _direction;
        private float   _speed;
        private float   _damage;
        private DamageType _damageType;
        private Entity  _owner;
        private StatusEffectApplicationPayload[] _statusPayloads;
        private ProjectilePoolService _poolService;
        private GameObject _poolPrefabKey;
        private string _runtimePoolKey;
        private float _remainingLifetime;

        public void BindPool(ProjectilePoolService poolService, GameObject prefabKey, string runtimePoolKey)
        {
            _poolService = poolService;
            _poolPrefabKey = prefabKey;
            _runtimePoolKey = runtimePoolKey;
        }

        public void Launch(
            Vector3 direction,
            float damage,
            float speed,
            DamageType damageType,
            Entity owner,
            StatusEffectApplicationPayload[] statusPayloads)
        {
            _direction = direction;
            _damage    = damage;
            _speed     = speed;
            _damageType = damageType;
            _owner     = owner;
            _statusPayloads = statusPayloads;
            _remainingLifetime = _lifetime;

            transform.rotation = Quaternion.LookRotation(_direction);
        }

        private void Update()
        {
            if (_remainingLifetime > 0f)
            {
                _remainingLifetime -= Time.deltaTime;
                if (_remainingLifetime <= 0f)
                {
                    Release();
                    return;
                }
            }

            transform.Translate(Vector3.forward * (_speed * Time.deltaTime));
        }

        private void OnDisable()
        {
            _remainingLifetime = 0f;
            _owner = null;
            _statusPayloads = null;
            _speed = 0f;
            _damage = 0f;
        }

        private void OnTriggerEnter(Collider other)
        {
            Entity entity = other.GetComponent<Entity>() ?? other.GetComponentInParent<Entity>();
            if (entity == null)
            {
                Release();
                return;
            }

            if (!EntityCombatUtility.AreHostile(_owner, entity)) return;
            
            entity.HealthSystem.TakeDamage(_damage, _damageType);

            if (_statusPayloads != null)
            {
                foreach (StatusEffectApplicationPayload payload in _statusPayloads)
                    entity.StatusEffectSystem.ApplyStatus(payload);
            }

            Release();
        }

        private void Release()
        {
            if (_poolService != null)
            {
                _poolService.Release(this, _poolPrefabKey, _runtimePoolKey);
                return;
            }

            Destroy(gameObject);
        }
    }
}
