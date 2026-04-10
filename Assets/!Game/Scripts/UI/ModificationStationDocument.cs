using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Interactables;
using _Game.Scripts.Services;
using _Game.Scripts.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Scripts.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class ModificationStationDocument : MonoBehaviour
    {
        [SerializeField] private ModificationStationNpc _station;

        private UIDocument _document;
        private ModificationLoadoutController _controller;
        private ModificationScreenService _screenService;
        private bool _isSubscribed;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _station ??= GetComponent<ModificationStationNpc>();

            _controller = new ModificationLoadoutController(_document.rootVisualElement, CloseScreen);
            _controller.Hide();
        }

        private void Start()
        {
            _screenService = ResolveScreenService();
            _controller.BindScreenService(_screenService);
            SubscribeToScreenService();
        }

        private void OnEnable()
        {
            SubscribeToScreenService();
        }

        private void OnDisable()
        {
            if (_screenService == null || !_isSubscribed)
                return;

            _screenService.OpenRequested -= OnOpenRequested;
            _screenService.Closed -= OnClosed;
            _isSubscribed = false;
        }

        private void OnDestroy() => _controller?.Dispose();

        private void OnOpenRequested(ModificationStationNpc station)
        {
            if (_station == null || station != _station)
            {
                _controller.Hide();
                return;
            }

            _controller.Open();
        }

        private void OnClosed() => _controller.Hide();

        private void CloseScreen()
        {
            if (_screenService == null)
                return;

            _screenService.Close();
        }

        private ModificationScreenService ResolveScreenService()
        {
            if (ServiceLocator.Instance == null || !ServiceLocator.Instance.Has<ModificationScreenService>())
            {
                Debug.LogWarning("[ModificationStationDocument] ModificationScreenService is not available yet.", this);
                return null;
            }

            return ServiceLocator.Instance.Get<ModificationScreenService>();
        }

        private void SubscribeToScreenService()
        {
            if (_isSubscribed)
                return;

            _screenService ??= ResolveScreenService();
            if (_screenService == null)
                return;

            _screenService.OpenRequested += OnOpenRequested;
            _screenService.Closed += OnClosed;
            _isSubscribed = true;
        }
    }
}
