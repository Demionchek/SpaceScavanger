using System;
using System.Collections.Generic;
using Game.Core;
using Game.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public sealed class InventoryUI : MonoBehaviour
    {
        private static readonly ResourceType[] AllResources =
            (ResourceType[])Enum.GetValues(typeof(ResourceType));

        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _resourcesContainer;
        [SerializeField] private Transform _itemsContainer;
        [SerializeField] private Transform _reputationContainer;
        [SerializeField] private GameObject _rowPrefab;
        [SerializeField] private Button _closeButton;
        [SerializeField] private ResourceIconSet _resourceIconSet;
        [SerializeField] private TMP_Text _emptyText;
        [SerializeField] private GameObject _resourcesView;
        [SerializeField] private GameObject _itemsView;
        [SerializeField] private GameObject _reputationView;
        [SerializeField] private Button _resourcesTab;
        [SerializeField] private Button _itemsTab;
        [SerializeField] private Button _reputationTab;

        private readonly List<GameObject> _rows = new();
        private InventoryTab _tab;
        private int _resourceRowCount;
        private int _itemRowCount;

        private EventBus _eventBus;
        private IResourceService _resourceService;
        private IItemService _itemService;
        private IReputationService _reputationService;
        private IPauseService _pauseService;
        private bool _open;

        [Inject]
        public void Construct(
            EventBus eventBus,
            IResourceService resourceService,
            IItemService itemService,
            IReputationService reputationService,
            IPauseService pauseService)
        {
            _eventBus = eventBus;
            _resourceService = resourceService;
            _itemService = itemService;
            _reputationService = reputationService;
            _pauseService = pauseService;

            _eventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
            _eventBus.Subscribe<ItemChangedEvent>(OnItemChanged);
            _eventBus.Subscribe<ReputationChangedEvent>(OnReputationChanged);
        }

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }

            AddTabListener(_resourcesTab, InventoryTab.Resources);
            AddTabListener(_itemsTab, InventoryTab.Items);
            AddTabListener(_reputationTab, InventoryTab.Reputation);
            ShowTab(InventoryTab.Resources);

            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void AddTabListener(Button button, InventoryTab tab)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => ShowTab(tab));
            }
        }

        private void ShowTab(InventoryTab tab)
        {
            _tab = tab;

            SetView(_resourcesView, _resourcesTab, InventoryTab.Resources);
            SetView(_itemsView, _itemsTab, InventoryTab.Items);
            SetView(_reputationView, _reputationTab, InventoryTab.Reputation);
            UpdateEmptyText();
        }

        private void SetView(GameObject view, Button tabButton, InventoryTab tab)
        {
            var active = _tab == tab;

            if (view != null)
            {
                view.SetActive(active);
            }

            if (tabButton != null)
            {
                tabButton.interactable = !active;
            }
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
            _eventBus?.Unsubscribe<ItemChangedEvent>(OnItemChanged);
            _eventBus?.Unsubscribe<ReputationChangedEvent>(OnReputationChanged);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.iKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        private void Toggle()
        {
            if (_open)
            {
                Close();
                return;
            }

            _open = true;

            if (_panel != null)
            {
                _panel.SetActive(true);
            }

            ShowTab(InventoryTab.Resources);
            Refresh();
            _pauseService.RequestPause();
        }

        private void Close()
        {
            if (!_open)
            {
                return;
            }

            _open = false;

            if (_panel != null)
            {
                _panel.SetActive(false);
            }

            _pauseService.ReleasePause();
        }

        private void OnResourceChanged(ResourceChangedEvent _) => RefreshIfOpen();

        private void OnItemChanged(ItemChangedEvent _) => RefreshIfOpen();

        private void OnReputationChanged(ReputationChangedEvent _) => RefreshIfOpen();

        private void RefreshIfOpen()
        {
            if (_open)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            ClearRows();

            _resourceRowCount = BuildResourceRows();
            _itemRowCount = BuildItemRows();
            BuildReputationRows();

            UpdateEmptyText();
        }

        private void UpdateEmptyText()
        {
            if (_emptyText == null)
            {
                return;
            }

            var empty = _tab switch
            {
                InventoryTab.Resources => _resourceRowCount == 0,
                InventoryTab.Items => _itemRowCount == 0,
                _ => false
            };

            _emptyText.gameObject.SetActive(empty);
            _emptyText.text = _tab == InventoryTab.Items ? "No items" : "Cargo hold is empty";
        }

        private int BuildResourceRows()
        {
            if (_resourcesContainer == null)
            {
                return 0;
            }

            var count = 0;

            foreach (var type in AllResources)
            {
                var amount = _resourceService.GetAmount(type);
                if (amount <= 0)
                {
                    continue;
                }

                var icon = _resourceIconSet != null ? _resourceIconSet.GetIcon(type) : null;
                AddRow(_resourcesContainer, icon, type.ToString(), $"x{amount}");
                count++;
            }

            return count;
        }

        private int BuildItemRows()
        {
            if (_itemsContainer == null)
            {
                return 0;
            }

            var count = 0;

            foreach (var pair in _itemService.All)
            {
                if (pair.Key == null || pair.Value <= 0)
                {
                    continue;
                }

                AddRow(_itemsContainer, pair.Key.Icon, pair.Key.DisplayName, $"x{pair.Value}");
                count++;
            }

            return count;
        }

        private void BuildReputationRows()
        {
            if (_reputationContainer == null)
            {
                return;
            }

            foreach (var pair in _reputationService.All)
            {
                var name = string.IsNullOrEmpty(pair.Key.DisplayName) ? pair.Key.name : pair.Key.DisplayName;
                AddRow(_reputationContainer, pair.Key.Icon, name, FormatReputation(pair.Value));
            }
        }

        private static string FormatReputation(int value)
        {
            var (label, color) = value switch
            {
                <= -50 => ("Hostile", "#FF6B6B"),
                < 0 => ("Unfriendly", "#FFA76B"),
                < 25 => ("Neutral", "#CFCFCF"),
                < 75 => ("Friendly", "#7CFF7C"),
                _ => ("Allied", "#6BD3FF")
            };

            return $"<color={color}>{value:+#;-#;0} {label}</color>";
        }

        private void AddRow(Transform container, Sprite icon, string name, string amount)
        {
            if (_rowPrefab == null)
            {
                return;
            }

            var row = Instantiate(_rowPrefab, container);
            _rows.Add(row);
            row.GetComponent<InventoryRowUI>().Set(icon, name, amount, null, null);
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
            {
                Destroy(row);
            }

            _rows.Clear();
        }

        private enum InventoryTab
        {
            Resources,
            Items,
            Reputation
        }
    }
}
