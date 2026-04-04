using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Systems.StatusEffects;
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
        private Entity  _owner;
        private StatusEffectApplicationPayload[] _statusPayloads;

        public void Launch(
            Vector3 direction,
            float damage,
            float speed,
            Entity owner,
            StatusEffectApplicationPayload[] statusPayloads)
        {
            _direction = direction;
            _damage    = damage;
            _speed     = speed;
            _owner     = owner;
            _statusPayloads = statusPayloads;

            transform.rotation = Quaternion.LookRotation(_direction);
            Destroy(gameObject, _lifetime);
        }

        private void Update()
        {
            transform.Translate(Vector3.forward * (_speed * Time.deltaTime));
        }

        private void OnTriggerEnter(Collider other)
        {
            var entity = other.GetComponent<Entity>();
            if (entity == null || entity == _owner) return;
            
            entity.HealthSystem.TakeDamage(_damage);

            if (_statusPayloads != null)
            {
                foreach (StatusEffectApplicationPayload payload in _statusPayloads)
                    entity.StatusEffectSystem.ApplyStatus(payload);
            }

            Destroy(gameObject);
        }
    }
}
