using UnityEngine;
using UnityEngine.InputSystem;

namespace _Game.Scripts.Gameplay.TopDownCamera
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 offset;

        [SerializeField] private float followSmoothTime;

        [SerializeField] private float aimWeight;
        [SerializeField] private float maxAimOffset;
        [SerializeField] private float aimSmoothTime;

        private Camera camera;
        private Transform _target;
        private Vector3 followVelocity;
        private Vector3 aimVelocity;
        private Vector3 currentAimOffset;

        private void Awake() => camera = GetComponent<Camera>();

        public void SetTarget(Transform target) => _target = target;

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 aimWorld = CursorToWorldPlane(_target.position.y);
            Vector3 raw = aimWorld - _target.position;
            raw.y = 0f;

            Vector3 desiredAim = Vector3.ClampMagnitude(raw * aimWeight, maxAimOffset);
            currentAimOffset = Vector3.SmoothDamp(currentAimOffset, desiredAim,
                ref aimVelocity, aimSmoothTime);

            Vector3 desired = _target.position + currentAimOffset + offset;
            transform.position = Vector3.SmoothDamp(
                transform.position, desired,
                ref followVelocity, followSmoothTime);
        }

        private Vector3 CursorToWorldPlane(float planeY)
        {
            Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Mathf.Abs(ray.direction.y) < 1e-4f) return _target.position;
            float t = (planeY - ray.origin.y) / ray.direction.y;
            return ray.origin + ray.direction * t;
        }
    }
}