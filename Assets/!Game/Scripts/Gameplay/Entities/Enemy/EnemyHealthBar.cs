using System.Collections;
using _Game.Scripts.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace _Game.Scripts.Gameplay.Entities.Enemy
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 2.2f, 0f);

        private const float BarWidth     = 64f;
        private const float HideDelay    = 3f;
        private const float FadeDuration = 0.4f;

        // ── Статический кеш raycast — вычисляется один раз в кадр ────────
        private static int     _cacheFrame = -1;
        private static Collider _cacheCollider;

        private static Collider GetHoveredCollider(Camera cam)
        {
            if (Time.frameCount == _cacheFrame) return _cacheCollider;
            _cacheFrame = Time.frameCount;
            var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            _cacheCollider = Physics.Raycast(ray, out var hit) ? hit.collider : null;
            return _cacheCollider;
        }
        // ─────────────────────────────────────────────────────────────────

        [SerializeField] private VisualTreeAsset _template;

        private VisualElement _container;
        private VisualElement _root;
        private VisualElement _fill;
        private Camera        _camera;
        private Entity        _entity;
        private Coroutine     _hideCoroutine;
        private bool          _isHovered;

        private void Start()
        {
            _camera = Camera.main;
            _entity = GetComponent<Entity>();

            var layer = Object.FindFirstObjectByType<EnemyBarsLayer>();
            if (layer == null)
            {
                Debug.LogError("[EnemyHealthBar] EnemyBarsLayer not found in scene.");
                enabled = false;
                return;
            }

            _container = _template.Instantiate();
            layer.Root.Add(_container);

            _root = _container.Q("enemy-health-bar");
            _fill = _container.Q("enemy-health-bar__fill");

            _root.style.display = DisplayStyle.None;
            _root.style.opacity = 1f;

            _entity.HealthSystem.OnHealthChanged += OnHealthChanged;
        }

        private void OnDestroy()
        {
            if (_entity != null)
                _entity.HealthSystem.OnHealthChanged -= OnHealthChanged;
            _container?.RemoveFromHierarchy();
        }

        private void Update()
        {
            if (_camera == null) return;

            bool hovered = IsMouseOver();

            if (hovered && !_isHovered)
            {
                _isHovered = true;
                Show();
            }
            else if (!hovered && _isHovered)
            {
                _isHovered = false;
                StartHideTimer();
            }
        }

        private void LateUpdate()
        {
            if (_root == null || _root.style.display == DisplayStyle.None) return;

            var screenPos = _camera.WorldToScreenPoint(transform.position + _worldOffset);
            if (screenPos.z < 0f) { _root.style.display = DisplayStyle.None; return; }

            _root.style.left = screenPos.x - BarWidth * 0.5f;
            _root.style.top  = Screen.height - screenPos.y;
        }

        private void OnHealthChanged(float current, float max)
        {
            _fill.style.width = Length.Percent(max > 0f ? current / max * 100f : 0f);
            Show();
        }

        // ── Visibility ───────────────────────────────────────────────────
        private void Show()
        {
            if (_hideCoroutine != null) { StopCoroutine(_hideCoroutine); _hideCoroutine = null; }
            _root.style.display = DisplayStyle.Flex;
            _root.style.opacity = 1f;
        }

        private void StartHideTimer()
        {
            if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
            _hideCoroutine = StartCoroutine(HideRoutine());
        }

        private IEnumerator HideRoutine()
        {
            yield return new WaitForSeconds(HideDelay);

            for (float t = 0f; t < FadeDuration; t += Time.deltaTime)
            {
                _root.style.opacity = Mathf.Lerp(1f, 0f, t / FadeDuration);
                yield return null;
            }

            _root.style.display = DisplayStyle.None;
            _root.style.opacity = 1f;
            _isHovered = false;
        }

        // ── Hover detection ──────────────────────────────────────────────
        private bool IsMouseOver()
        {
            var hit = GetHoveredCollider(_camera);
            return hit != null &&
                   (hit.transform == transform || hit.transform.IsChildOf(transform));
        }
    }
}
