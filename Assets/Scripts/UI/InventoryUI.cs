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

        [Tooltip("Высота строки-заголовка раздела в списке репутации.")]
        [SerializeField] private float _headerHeight = 70f;

        [SerializeField] private float _headerFontSize = 40f;
        [SerializeField] private Color _headerColor = new(0.65f, 0.7f, 0.78f, 1f);

        [Tooltip("Чувствительность колеса: у префаба она 1, что при высоте канваса " +
                 "в 2160 почти не прокручивает.")]
        [SerializeField] private float _scrollSensitivity = 60f;

        private readonly List<GameObject> _rows = new();
        private ScrollRect _scroll;
        private InventoryTab _tab;
        private int _resourceRowCount;
        private int _itemRowCount;

        private EventBus _eventBus;
        private IResourceService _resourceService;
        private IItemService _itemService;
        private IReputationService _reputationService;
        private IRecipeService _recipeService;
        private IRelationshipService _relationshipService;
        private IPauseService _pauseService;
        private bool _open;

        [Inject]
        public void Construct(
            EventBus eventBus,
            IResourceService resourceService,
            IItemService itemService,
            IReputationService reputationService,
            IRecipeService recipeService,
            IRelationshipService relationshipService,
            IPauseService pauseService)
        {
            _eventBus = eventBus;
            _resourceService = resourceService;
            _itemService = itemService;
            _reputationService = reputationService;
            _recipeService = recipeService;
            _relationshipService = relationshipService;
            _pauseService = pauseService;

            _eventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
            _eventBus.Subscribe<ItemChangedEvent>(OnItemChanged);
            _eventBus.Subscribe<ReputationChangedEvent>(OnReputationChanged);
            _eventBus.Subscribe<RecipeLearnedEvent>(OnRecipeLearned);
            _eventBus.Subscribe<RelationshipChangedEvent>(OnRelationshipChanged);
            _eventBus.Subscribe<InventoryWindowRequestedEvent>(OnWindowRequested);
        }

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }

            EnsureScroll();
            AddTabListener(_resourcesTab, InventoryTab.Resources);
            AddTabListener(_itemsTab, InventoryTab.Items);
            AddTabListener(_reputationTab, InventoryTab.Reputation);
            ShowTab(InventoryTab.Resources);

            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        // Прокручивались только ресурсы: ScrollRect держит одно content, а вкладок
        // три. Каждому списку нужен фиттер — без него высота остаётся от вьюпорта,
        // и прокручивать нечего, — а на переключении вкладки content подменяется.
        private void EnsureScroll()
        {
            _scroll = _resourcesContainer != null
                ? _resourcesContainer.GetComponentInParent<ScrollRect>()
                : null;

            if (_scroll == null)
            {
                return;
            }

            _scroll.scrollSensitivity = Mathf.Max(_scroll.scrollSensitivity, _scrollSensitivity);
            PrepareContent(_resourcesContainer);
            PrepareContent(_itemsContainer);
            PrepareContent(_reputationContainer);
        }

        private static void PrepareContent(Transform container)
        {
            if (container is not RectTransform rect)
            {
                return;
            }

            // Верхняя растяжка: ширину держат якоря, высоту считает фиттер.
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 0f);

            if (rect.GetComponent<ContentSizeFitter>() == null)
            {
                var fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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

            if (_scroll != null && ActiveContainer() is RectTransform content)
            {
                _scroll.content = content;
                _scroll.verticalNormalizedPosition = 1f;
            }

            UpdateEmptyText();
        }

        private Transform ActiveContainer() => _tab switch
        {
            InventoryTab.Resources => _resourcesContainer,
            InventoryTab.Items => _itemsContainer,
            _ => _reputationContainer
        };

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
            _eventBus?.Unsubscribe<RecipeLearnedEvent>(OnRecipeLearned);
            _eventBus?.Unsubscribe<RelationshipChangedEvent>(OnRelationshipChanged);
            _eventBus?.Unsubscribe<InventoryWindowRequestedEvent>(OnWindowRequested);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.iKey.wasPressedThisFrame)
            {
                Toggle();
            }
            else if (_open && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        // Сундук в трюме открывает то же окно, что и клавиша.
        private void OnWindowRequested(InventoryWindowRequestedEvent _) => Open();

        private void Toggle()
        {
            if (_open)
            {
                Close();
                return;
            }

            Open();
        }

        private void Open()
        {
            if (_open)
            {
                return;
            }

            // Поверх чужого окна инвентарь не открывается: паузу держит кто-то
            // другой — диалог, торговец, мини-игра, — и буква "i" в наборе слов не
            // должна выдёргивать игрока из неё.
            if (_pauseService.IsPaused)
            {
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

        private void OnRecipeLearned(RecipeLearnedEvent _) => RefreshIfOpen();

        private void OnRelationshipChanged(RelationshipChangedEvent _) => RefreshIfOpen();

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

            // Чертёж — такое же имущество: торговец продаёт их как товар, а увидеть
            // свои можно было только в верстаке, среди того, что собирается прямо
            // сейчас. Правая колонка и отличает схему от самой детали.
            foreach (var recipe in _recipeService.Known)
            {
                if (recipe == null || recipe.Output == null)
                {
                    continue;
                }

                AddRow(_itemsContainer, recipe.Output.Icon, recipe.Output.DisplayName, "Schematic");
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

            var header = true;
            foreach (var pair in _reputationService.All)
            {
                if (header)
                {
                    header = false;
                    AddHeader(_reputationContainer, "FACTIONS");
                }

                var name = string.IsNullOrEmpty(pair.Key.DisplayName) ? pair.Key.name : pair.Key.DisplayName;
                AddRow(_reputationContainer, pair.Key.Icon, name, FormatReputation(pair.Value));
            }

            // Личное отношение жило только в короткой строке над диалогом: спросить
            // "а как ко мне относится Ви" было негде. Заголовок печатается лишь
            // когда есть кому стоять под ним.
            header = true;
            foreach (var pair in _relationshipService.All)
            {
                if (pair.Key == null)
                {
                    continue;
                }

                if (header)
                {
                    header = false;
                    AddHeader(_reputationContainer, "PEOPLE");
                }

                var name = string.IsNullOrEmpty(pair.Key.DisplayName) ? pair.Key.name : pair.Key.DisplayName;
                AddRow(_reputationContainer, pair.Key.Icon, name, FormatAttitude(pair.Value));
            }
        }

        // У человека своя шкала слов: "Allied" про одного пилота звучит как про флот.
        private static string FormatAttitude(int value)
        {
            var (label, color) = value switch
            {
                <= -50 => ("Hates you", "#FF6B6B"),
                < 0 => ("Cold", "#FFA76B"),
                < 25 => ("Neutral", "#CFCFCF"),
                < 75 => ("Warm", "#7CFF7C"),
                _ => ("Loyal", "#6BD3FF")
            };

            return $"<color={color}>{value:+#;-#;0} {label}</color>";
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

        // Разделитель, а не пункт списка: у него нет ни плашки, ни иконки, только
        // подпись. Шрифт одалживается у готового текста окна — иначе TMP подставит
        // свой и строка выпадет из пиксельного вида.
        private void AddHeader(Transform container, string text)
        {
            var holder = new GameObject("Header", typeof(RectTransform));
            holder.transform.SetParent(container, false);
            _rows.Add(holder);

            holder.AddComponent<LayoutElement>().preferredHeight = _headerHeight;

            var label = holder.AddComponent<TextMeshProUGUI>();
            if (_emptyText != null)
            {
                label.font = _emptyText.font;
                label.fontSharedMaterial = _emptyText.fontSharedMaterial;
            }

            label.fontSize = _headerFontSize;
            label.color = _headerColor;
            label.alignment = TextAlignmentOptions.Left;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.margin = new Vector4(24f, 0f, 0f, 0f);
            label.text = text;
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
