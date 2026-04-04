using System;
using System.Collections.Generic;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities.Player;
using _Game.Scripts.Gameplay.Systems.Modifications;
using _Game.Scripts.Gameplay.Systems.Stats;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Scripts.UI.Controllers
{
    public class ModificationLoadoutController
    {
        private enum DragSourceKind
        {
            None,
            Inventory,
            Slot
        }

        private sealed class SlotView
        {
            public VisualElement Root;
            public Label Title;
            public Label Meta;
        }

        private const string HiddenPanelClass = "mod-window-panel--hidden";
        private const string ActiveTabClass = "mod-window-tab-button--active";
        private const string SelectedCardClass = "mod-card-button--selected";
        private const string ErrorStatusClass = "mod-window-status--error";
        private const string FilledSlotClass = "mod-slot-button--filled";
        private const string EmptySlotClass = "mod-slot-button--empty";
        private const string SlotDropTargetClass = "mod-slot-button--drop-target";
        private const string InventoryDropTargetClass = "mod-window-inventory-section--drop-target";
        private const string DragGhostClass = "mod-drag-ghost";

        private readonly VisualElement _root;
        private readonly Action _closeRequested;
        private readonly VisualElement _overlay;
        private readonly Label _titleLabel;
        private readonly Button _closeButton;
        private readonly Button _loadoutTabButton;
        private readonly Button _upgradeTabButton;
        private readonly VisualElement _loadoutPanel;
        private readonly VisualElement _upgradePanel;
        private readonly Label _capacityLabel;
        private readonly Label _statusLabel;
        private readonly VisualElement _slotGrid;
        private readonly ScrollView _statsContainer;
        private readonly ScrollView _inventoryScroll;
        private readonly VisualElement _inventorySection;
        private readonly Label _upgradeDescriptionLabel;
        private Label _dragGhost;

        private readonly List<SlotView> _slotViews = new();

        private Player _player;
        private ModificationCardInstance _selectedCard;
        private ModificationCardInstance _draggedCard;
        private VisualElement _activeDropTarget;
        private DragSourceKind _dragSourceKind;
        private int _dragSourceSlotIndex = -1;
        private int _activePointerId = -1;
        private Vector2 _pointerDownPosition;
        private bool _pointerPressed;
        private bool _isDragging;
        private bool _suppressNextClick;

        public ModificationLoadoutController(VisualElement root, Action closeRequested)
        {
            _root = root;
            _closeRequested = closeRequested;
            _overlay = root.Q("modification-window-overlay");
            _titleLabel = root.Q<Label>("modification-window-title");
            _closeButton = root.Q<Button>("modification-window-close-button");
            _loadoutTabButton = root.Q<Button>("modification-loadout-tab-button");
            _upgradeTabButton = root.Q<Button>("modification-upgrade-tab-button");
            _loadoutPanel = root.Q("modification-loadout-panel");
            _upgradePanel = root.Q("modification-upgrade-panel");
            _capacityLabel = root.Q<Label>("modification-capacity-label");
            _statusLabel = root.Q<Label>("modification-status-label");
            _slotGrid = root.Q("modification-slot-grid");
            _statsContainer = root.Q<ScrollView>("modification-stats-container");
            _inventoryScroll = root.Q<ScrollView>("modification-inventory-scroll");
            _inventorySection = root.Q("modification-inventory-section");
            _upgradeDescriptionLabel = root.Q<Label>("modification-upgrade-placeholder-label");

            if (_overlay == null)
            {
                Debug.LogError("[ModificationLoadoutController] Required UI elements are missing from the modifier window document.");
                return;
            }

            _dragGhost = new Label();
            _dragGhost.AddToClassList(DragGhostClass);
            _dragGhost.style.display = DisplayStyle.None;
            _dragGhost.pickingMode = PickingMode.Ignore;
            _root.Add(_dragGhost);

            _closeButton.clicked += OnCloseClicked;
            _loadoutTabButton.clicked += ShowLoadoutPanel;
            _upgradeTabButton.clicked += ShowUpgradePanel;
            _overlay.RegisterCallback<PointerMoveEvent>(OnOverlayPointerMove);
            _overlay.RegisterCallback<PointerUpEvent>(OnOverlayPointerUp);

            BuildSlots();
            ShowLoadoutPanel();
            Hide();

            EventBus.Subscribe<OnModificationCardAddedEvent>(OnModificationChanged);
            EventBus.Subscribe<OnModificationInstalledEvent>(OnModificationChanged);
            EventBus.Subscribe<OnModificationRemovedEvent>(OnModificationChanged);
        }

        public void Dispose()
        {
            if (_overlay == null)
                return;

            _closeButton.clicked -= OnCloseClicked;
            _loadoutTabButton.clicked -= ShowLoadoutPanel;
            _upgradeTabButton.clicked -= ShowUpgradePanel;
            _overlay.UnregisterCallback<PointerMoveEvent>(OnOverlayPointerMove);
            _overlay.UnregisterCallback<PointerUpEvent>(OnOverlayPointerUp);

            EventBus.Unsubscribe<OnModificationCardAddedEvent>(OnModificationChanged);
            EventBus.Unsubscribe<OnModificationInstalledEvent>(OnModificationChanged);
            EventBus.Unsubscribe<OnModificationRemovedEvent>(OnModificationChanged);
        }

        public void Open(Player player)
        {
            _player = player;
            _selectedCard = null;
            Show();
            ShowLoadoutPanel();
            RefreshAll();
            SetStatus("Drag a modifier into a slot. Drag from a slot back into the inventory to remove it.");
        }

        public void Hide()
        {
            EndDrag();
            _selectedCard = null;
            _player = null;
            HideInternal();
        }

        private void OnCloseClicked() => _closeRequested?.Invoke();

        private void Show()
        {
            _root.pickingMode = PickingMode.Position;
            _overlay.style.display = DisplayStyle.Flex;
            _overlay.pickingMode = PickingMode.Position;
        }

        private void HideInternal()
        {
            _root.pickingMode = PickingMode.Ignore;
            _overlay.style.display = DisplayStyle.None;
            _overlay.pickingMode = PickingMode.Ignore;
        }

        private void ShowLoadoutPanel() => SetTabState(showLoadout: true);

        private void ShowUpgradePanel() => SetTabState(showLoadout: false);

        private void SetTabState(bool showLoadout)
        {
            if (_loadoutPanel == null || _upgradePanel == null)
                return;

            ToggleClass(_loadoutPanel, HiddenPanelClass, !showLoadout);
            ToggleClass(_upgradePanel, HiddenPanelClass, showLoadout);
            ToggleClass(_loadoutTabButton, ActiveTabClass, showLoadout);
            ToggleClass(_upgradeTabButton, ActiveTabClass, !showLoadout);
        }

        private void BuildSlots()
        {
            _slotGrid.Clear();
            _slotViews.Clear();

            for (int i = 0; i < ModificationLoadoutSystem.MaxSlots; i++)
            {
                int slotIndex = i;

                var root = new VisualElement();
                root.AddToClassList("mod-slot-button");
                root.RegisterCallback<PointerDownEvent>(evt => OnSlotPointerDown(evt, slotIndex));
                root.RegisterCallback<ClickEvent>(_ => OnSlotClicked(slotIndex));

                var title = new Label();
                title.AddToClassList("mod-slot-title");

                var meta = new Label();
                meta.AddToClassList("mod-slot-meta");

                root.Add(title);
                root.Add(meta);

                _slotViews.Add(new SlotView
                {
                    Root = root,
                    Title = title,
                    Meta = meta
                });

                _slotGrid.Add(root);
            }
        }

        private void RefreshAll()
        {
            if (_player == null)
                return;

            RefreshHeader();
            RefreshSlots();
            RefreshStats();
            RefreshInventory();
            RefreshUpgradePlaceholder();
        }

        private void RefreshHeader()
        {
            _titleLabel.text = $"{_player.name} Modifications";
            _capacityLabel.text = $"Capacity {GetUsedCapacity()} / {_player.ModificationLoadoutSystem.Capacity}";
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < _slotViews.Count; i++)
            {
                SlotView slot = _slotViews[i];
                ModificationCardInstance card = _player.ModificationLoadoutSystem.GetCardInSlot(i);

                slot.Root.RemoveFromClassList(EmptySlotClass);
                slot.Root.RemoveFromClassList(FilledSlotClass);
                slot.Root.RemoveFromClassList(SlotDropTargetClass);

                if (card == null)
                {
                    slot.Title.text = $"Slot {i + 1}";
                    slot.Meta.text = "Empty";
                    slot.Root.tooltip = "Drag a modifier here, or click with a selected card to install it.";
                    slot.Root.AddToClassList(EmptySlotClass);
                    continue;
                }

                slot.Title.text = card.Definition.DisplayName;
                slot.Meta.text = $"Rank {card.Definition.Rank}   Cost {card.Definition.InstallCost}";
                slot.Root.tooltip = $"Drag to another slot or back to the inventory list. Click to remove '{card.Definition.DisplayName}'.";
                slot.Root.AddToClassList(FilledSlotClass);
            }
        }

        private void RefreshStats()
        {
            _statsContainer.contentContainer.Clear();

            StatsSystem stats = _player.StatsSystem;

            AddStatRow("Max Health", stats.MaxHealth.Value);
            AddStatRow("Move Speed", stats.MoveSpeed.Value);
            AddStatRow("Armor", stats.Armor.Value);
            AddStatRow("Magic Resist", stats.MagicResistance.Value);
            AddStatRow("Elemental Resist", stats.ElementalResistance.Value);
            AddStatRow("Attack Damage", stats.AttackDamage.Value);
            AddStatRow("Attack Range", stats.AttackRange.Value);
            AddStatRow("Attack Rate", stats.AttackRate.Value);
            AddStatRow("Attack Type", stats.AttackDamageType.ToString());
        }

        private void RefreshInventory()
        {
            _inventoryScroll.contentContainer.Clear();
            _inventorySection.RemoveFromClassList(InventoryDropTargetClass);

            foreach (ModificationCardInstance card in _player.ModificationInventory.Cards)
            {
                if (GetInstalledSlotIndex(card) >= 0)
                    continue;

                VisualElement cardElement = CreateInventoryCardElement(card);
                _inventoryScroll.Add(cardElement);
            }
        }

        private VisualElement CreateInventoryCardElement(ModificationCardInstance card)
        {
            var root = new VisualElement();
            root.AddToClassList("mod-card-button");
            root.tooltip = card.Definition.Description;
            root.RegisterCallback<PointerDownEvent>(evt => OnInventoryPointerDown(evt, card));
            root.RegisterCallback<ClickEvent>(_ => OnInventoryCardClicked(card));

            if (ReferenceEquals(_selectedCard, card))
                root.AddToClassList(SelectedCardClass);

            var title = new Label(card.Definition.DisplayName);
            title.AddToClassList("mod-card-title");

            var meta = new Label($"Rank {card.Definition.Rank}   Cost {card.Definition.InstallCost}");
            meta.AddToClassList("mod-card-meta");

            root.Add(title);
            root.Add(meta);

            return root;
        }

        private void RefreshUpgradePlaceholder()
        {
            _upgradeDescriptionLabel.text =
                "Upgrade panel placeholder.\nYou can wire upgrade widgets into this panel later without changing the loadout flow.";
        }

        private void OnSlotClicked(int slotIndex)
        {
            if (ConsumeSuppressedClick() || _player == null)
                return;

            ModificationCardInstance installedCard = _player.ModificationLoadoutSystem.GetCardInSlot(slotIndex);
            if (installedCard != null)
            {
                if (_player.ModificationLoadoutSystem.TryRemove(slotIndex, out string removeError))
                {
                    if (ReferenceEquals(_selectedCard, installedCard))
                        _selectedCard = null;

                    RefreshAll();
                    SetStatus($"Removed '{installedCard.Definition.DisplayName}' from slot {slotIndex + 1}.");
                    return;
                }

                SetStatus(removeError, true);
                return;
            }

            if (_selectedCard == null)
            {
                SetStatus("Select a card first, or drag one into the slot.", true);
                return;
            }

            if (_player.ModificationLoadoutSystem.TryInstall(_selectedCard, slotIndex, out string installError))
            {
                string installedName = _selectedCard.Definition.DisplayName;
                _selectedCard = null;
                RefreshAll();
                SetStatus($"Installed '{installedName}' into slot {slotIndex + 1}.");
                return;
            }

            SetStatus(installError, true);
        }

        private void OnInventoryCardClicked(ModificationCardInstance card)
        {
            if (ConsumeSuppressedClick() || _player == null || card == null)
                return;

            _selectedCard = ReferenceEquals(_selectedCard, card) ? null : card;
            RefreshInventory();

            if (_selectedCard == null)
            {
                SetStatus("Card selection cleared.");
                return;
            }

            SetStatus($"Selected '{card.Definition.DisplayName}'. Click a slot or drag it into one.");
        }

        private void OnInventoryPointerDown(PointerDownEvent evt, ModificationCardInstance card)
        {
            if (_player == null || card == null)
                return;

            BeginDrag(evt, card, DragSourceKind.Inventory, -1);
        }

        private void OnSlotPointerDown(PointerDownEvent evt, int slotIndex)
        {
            if (_player == null)
                return;

            ModificationCardInstance card = _player.ModificationLoadoutSystem.GetCardInSlot(slotIndex);
            if (card == null)
                return;

            BeginDrag(evt, card, DragSourceKind.Slot, slotIndex);
        }

        private void BeginDrag(PointerDownEvent evt, ModificationCardInstance card, DragSourceKind sourceKind, int slotIndex)
        {
            if (evt.button != 0)
                return;

            EndDrag();

            _draggedCard = card;
            _dragSourceKind = sourceKind;
            _dragSourceSlotIndex = slotIndex;
            _activePointerId = evt.pointerId;
            _pointerDownPosition = evt.position;
            _pointerPressed = true;

            _overlay.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnOverlayPointerMove(PointerMoveEvent evt)
        {
            if (!_pointerPressed || evt.pointerId != _activePointerId || _draggedCard == null)
                return;

            if (!_isDragging)
            {
                if (Vector2.Distance(_pointerDownPosition, evt.position) < 10f)
                    return;

                StartDrag(evt.position);
            }

            UpdateDrag(evt.position);
            evt.StopPropagation();
        }

        private void OnOverlayPointerUp(PointerUpEvent evt)
        {
            if (!_pointerPressed || evt.pointerId != _activePointerId)
                return;

            bool didDrag = _isDragging;

            if (_overlay.HasPointerCapture(evt.pointerId))
                _overlay.ReleasePointer(evt.pointerId);

            if (didDrag)
            {
                HandleDrop(evt.position);
                _suppressNextClick = true;
            }

            EndDrag();
            evt.StopPropagation();
        }

        private void StartDrag(Vector2 pointerPosition)
        {
            _isDragging = true;
            _dragGhost.text = _draggedCard.Definition.DisplayName;
            _dragGhost.style.display = DisplayStyle.Flex;
            UpdateDrag(pointerPosition);
            SetStatus($"Dragging '{_draggedCard.Definition.DisplayName}'.");
        }

        private void UpdateDrag(Vector2 pointerPosition)
        {
            _dragGhost.style.left = pointerPosition.x + 18f;
            _dragGhost.style.top = pointerPosition.y + 18f;

            ClearActiveDropTarget();

            if (TryResolveSlotDropTarget(pointerPosition, out int slotIndex))
            {
                if (IsValidSlotDropTarget(slotIndex))
                {
                    _activeDropTarget = _slotViews[slotIndex].Root;
                    _activeDropTarget.AddToClassList(SlotDropTargetClass);
                }

                return;
            }

            if (_inventorySection.worldBound.Contains(pointerPosition) && IsValidInventoryDropTarget())
            {
                _activeDropTarget = _inventorySection;
                _activeDropTarget.AddToClassList(InventoryDropTargetClass);
            }
        }

        private void HandleDrop(Vector2 pointerPosition)
        {
            if (TryResolveSlotDropTarget(pointerPosition, out int slotIndex) && IsValidSlotDropTarget(slotIndex))
            {
                if (_dragSourceKind == DragSourceKind.Inventory)
                {
                    TryInstallDraggedCard(slotIndex);
                    return;
                }

                if (_dragSourceKind == DragSourceKind.Slot)
                {
                    TryMoveDraggedCard(slotIndex);
                    return;
                }
            }

            if (_inventorySection.worldBound.Contains(pointerPosition) && IsValidInventoryDropTarget())
            {
                TryRemoveDraggedCard();
                return;
            }

            SetStatus("Drop target is not valid for this modifier.", true);
        }

        private void TryInstallDraggedCard(int slotIndex)
        {
            if (_player.ModificationLoadoutSystem.TryInstall(_draggedCard, slotIndex, out string error))
            {
                string cardName = _draggedCard.Definition.DisplayName;
                _selectedCard = null;
                RefreshAll();
                SetStatus($"Installed '{cardName}' into slot {slotIndex + 1}.");
                return;
            }

            SetStatus(error, true);
        }

        private void TryRemoveDraggedCard()
        {
            if (_dragSourceKind != DragSourceKind.Slot)
            {
                SetStatus("Only installed modifiers can be returned to inventory.", true);
                return;
            }

            if (_player.ModificationLoadoutSystem.TryRemove(_dragSourceSlotIndex, out string error))
            {
                string cardName = _draggedCard.Definition.DisplayName;
                RefreshAll();
                SetStatus($"Returned '{cardName}' to the inventory.");
                return;
            }

            SetStatus(error, true);
        }

        private void TryMoveDraggedCard(int targetSlotIndex)
        {
            if (_dragSourceKind != DragSourceKind.Slot)
            {
                SetStatus("Only installed modifiers can be moved between slots.", true);
                return;
            }

            if (_dragSourceSlotIndex == targetSlotIndex)
            {
                SetStatus("Modifier stayed in the same slot.");
                return;
            }

            if (_player.ModificationLoadoutSystem.TryMove(_dragSourceSlotIndex, targetSlotIndex, out string error))
            {
                string cardName = _draggedCard.Definition.DisplayName;
                RefreshAll();
                SetStatus($"Moved '{cardName}' to slot {targetSlotIndex + 1}.");
                return;
            }

            SetStatus(error, true);
        }

        private bool TryResolveSlotDropTarget(Vector2 pointerPosition, out int slotIndex)
        {
            for (int i = 0; i < _slotViews.Count; i++)
            {
                if (!_slotViews[i].Root.worldBound.Contains(pointerPosition))
                    continue;

                slotIndex = i;
                return true;
            }

            slotIndex = -1;
            return false;
        }

        private bool IsValidSlotDropTarget(int slotIndex)
        {
            if (_player == null || _draggedCard == null || slotIndex < 0 || slotIndex >= _slotViews.Count)
                return false;

            ModificationCardInstance slotCard = _player.ModificationLoadoutSystem.GetCardInSlot(slotIndex);

            if (_dragSourceKind == DragSourceKind.Inventory)
                return slotCard == null;

            if (_dragSourceKind == DragSourceKind.Slot)
                return slotCard == null || slotIndex == _dragSourceSlotIndex;

            return false;
        }

        private bool IsValidInventoryDropTarget() =>
            _player != null && _draggedCard != null && _dragSourceKind == DragSourceKind.Slot;

        private int GetInstalledSlotIndex(ModificationCardInstance card)
        {
            if (card == null || _player == null)
                return -1;

            return _player.ModificationLoadoutSystem.TryGetSlotIndex(card, out int slotIndex)
                ? slotIndex
                : -1;
        }

        private int GetUsedCapacity() => _player?.ModificationLoadoutSystem.UsedCapacity ?? 0;

        private void AddStatRow(string label, float value) => AddStatRow(label, value.ToString("0.##"));

        private void AddStatRow(string label, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("mod-stat-row");

            var nameLabel = new Label(label);
            nameLabel.AddToClassList("mod-stat-name");

            var valueLabel = new Label(value);
            valueLabel.AddToClassList("mod-stat-value");

            row.Add(nameLabel);
            row.Add(valueLabel);
            _statsContainer.Add(row);
        }

        private void OnModificationChanged(OnModificationCardAddedEvent evt)
        {
            if (_player == null || evt.Entity != _player)
                return;

            RefreshAll();
        }

        private void OnModificationChanged(OnModificationInstalledEvent evt)
        {
            if (_player == null || evt.Entity != _player)
                return;

            RefreshAll();
        }

        private void OnModificationChanged(OnModificationRemovedEvent evt)
        {
            if (_player == null || evt.Entity != _player)
                return;

            RefreshAll();
        }

        private void SetStatus(string text, bool isError = false)
        {
            _statusLabel.text = text;
            ToggleClass(_statusLabel, ErrorStatusClass, isError);
        }

        private static void ToggleClass(VisualElement element, string className, bool enabled)
        {
            if (element == null)
                return;

            if (enabled)
                element.AddToClassList(className);
            else
                element.RemoveFromClassList(className);
        }

        private void EndDrag()
        {
            ClearActiveDropTarget();

            if (_dragGhost != null)
                _dragGhost.style.display = DisplayStyle.None;

            if (_activePointerId >= 0 && _overlay.HasPointerCapture(_activePointerId))
                _overlay.ReleasePointer(_activePointerId);

            _draggedCard = null;
            _dragSourceKind = DragSourceKind.None;
            _dragSourceSlotIndex = -1;
            _activePointerId = -1;
            _pointerPressed = false;
            _isDragging = false;
        }

        private void ClearActiveDropTarget()
        {
            if (_activeDropTarget == null)
                return;

            _activeDropTarget.RemoveFromClassList(SlotDropTargetClass);
            _activeDropTarget.RemoveFromClassList(InventoryDropTargetClass);
            _activeDropTarget = null;
        }

        private bool ConsumeSuppressedClick()
        {
            if (!_suppressNextClick)
                return false;

            _suppressNextClick = false;
            return true;
        }
    }
}
