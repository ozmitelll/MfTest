using _Game.Scripts.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Scripts.Gameplay.Interactables
{
    [RequireComponent(typeof(Portal))]
    public class PortalChargeBar : MonoBehaviour
    {
        private const float BarWidth = 96f;

        [SerializeField] private Vector3 _worldOffset = new(0f, 2.3f, 0f);
        [SerializeField] private VisualTreeAsset _template;

        private Camera _camera;
        private Portal _portal;
        private VisualElement _container;
        private VisualElement _root;
        private VisualElement _fill;
        private Label _statusLabel;

        private void Start()
        {
            _camera = Camera.main;
            _portal = GetComponent<Portal>();

            EnemyBarsLayer layer = Object.FindFirstObjectByType<EnemyBarsLayer>();
            if (layer == null)
            {
                Debug.LogError($"[{nameof(PortalChargeBar)}] EnemyBarsLayer not found in scene.", this);
                enabled = false;
                return;
            }

            if (_template == null)
            {
                Debug.LogError($"[{nameof(PortalChargeBar)}] Portal charge bar template is not assigned.", this);
                enabled = false;
                return;
            }

            _container = _template.Instantiate();
            layer.Root.Add(_container);

            _root = _container.Q("portal-charge-bar");
            _fill = _container.Q("portal-charge-bar__fill");
            _statusLabel = _container.Q<Label>("portal-charge-bar__status");

            if (_root != null)
                _root.style.display = DisplayStyle.None;

            SyncVisualState();
        }

        private void Update()
        {
            if (_camera == null)
                _camera = Camera.main;

            SyncVisualState();
        }

        private void LateUpdate()
        {
            if (_root == null || _camera == null || _portal == null || _root.style.display == DisplayStyle.None)
                return;

            Vector3 screenPos = _camera.WorldToScreenPoint(transform.position + _worldOffset);
            if (screenPos.z < 0f)
            {
                _root.style.display = DisplayStyle.None;
                return;
            }

            _root.style.left = screenPos.x - BarWidth * 0.5f;
            _root.style.top = Screen.height - screenPos.y;
        }

        private void OnDestroy()
        {
            _container?.RemoveFromHierarchy();
        }

        private void SyncVisualState()
        {
            if (_portal == null || _root == null || _fill == null)
                return;

            if (!_portal.ShouldShowChargeBar)
            {
                _root.style.display = DisplayStyle.None;
                return;
            }

            _root.style.display = DisplayStyle.Flex;
            _fill.style.width = Length.Percent(_portal.ChargeProgressNormalized * 100f);

            if (_statusLabel != null)
            {
                _statusLabel.text = _portal.IsCharged
                    ? "Charged"
                    : _portal.IsCharging
                        ? "Charging..."
                        : _portal.IsPlayerInsideChargeArea
                            ? "Charging..."
                            : "Stand in field";
            }
        }
    }
}