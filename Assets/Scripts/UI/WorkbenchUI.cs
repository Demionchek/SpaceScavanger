using System.Collections.Generic;
using System.Text;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public sealed class WorkbenchUI : MonoBehaviour
    {
        private const string Enough = "#8CE99A";
        private const string Short = "#FF8787";
        private const string Detail = "#A9B0B8";
        private const string Held = "#FFD43B";

        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _recipesContainer;
        [SerializeField] private GameObject _recipeRowPrefab;
        [SerializeField] private Transform _upgradesContainer;
        [SerializeField] private GameObject _upgradeRowPrefab;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _recipesView;
        [SerializeField] private GameObject _upgradesView;
        [Header("Вкладки")]
        [SerializeField] private Button _craftTab;

        [SerializeField] private TMP_Text _craftTabLabel;
        [SerializeField] private Button _upgradesTab;
        [SerializeField] private TMP_Text _upgradesTabLabel;

        [Tooltip("Открытая вкладка притушена: нажимать её некуда. Спрайт у обеих один.")]
        [SerializeField] private Color _openTab = new(0.5f, 0.56f, 0.62f, 1f);

        [SerializeField] private Color _closedTab = Color.white;

        private readonly List<GameObject> _rows = new();
        private readonly StringBuilder _costsBuilder = new();
        private readonly StringBuilder _titleBuilder = new();

        private EventBus _eventBus;
        private ICraftingService _craftingService;
        private IUpgradeService _upgradeService;
        private IResourceService _resourceService;
        private IItemService _itemService;
        private IRecipeService _recipeService;
        private IPauseService _pauseService;
        private bool _isOpen;
        private bool _showingUpgrades;

        [Inject]
        public void Construct(
            EventBus eventBus,
            ICraftingService craftingService,
            IUpgradeService upgradeService,
            IResourceService resourceService,
            IItemService itemService,
            IRecipeService recipeService,
            IPauseService pauseService)
        {
            _eventBus = eventBus;
            _craftingService = craftingService;
            _upgradeService = upgradeService;
            _resourceService = resourceService;
            _itemService = itemService;
            _recipeService = recipeService;
            _pauseService = pauseService;
        }

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
            _craftTab.onClick.AddListener(() => SelectTab(false));
            _upgradesTab.onClick.AddListener(() => SelectTab(true));

            Label(_craftTabLabel, "Craft");
            Label(_upgradesTabLabel, "Upgrades");

            _panel.SetActive(false);
        }

        private void SelectTab(bool upgrades)
        {
            _showingUpgrades = upgrades;
            UpdateTabView();
        }

        private void UpdateTabView()
        {
            _recipesView.SetActive(!_showingUpgrades);
            _upgradesView.SetActive(_showingUpgrades);

            ShowTab(_craftTab, _craftTabLabel, !_showingUpgrades);
            ShowTab(_upgradesTab, _upgradesTabLabel, _showingUpgrades);
        }

        // Открытая вкладка выключена: нажимать её некуда, и это же отличает её от
        // второй. Disabled-цвет кнопок на префабе выставлен в белый, иначе Unity
        // приглушила бы её ещё раз поверх нашего цвета.
        private void ShowTab(Button tab, TMP_Text label, bool open)
        {
            if (tab == null)
            {
                return;
            }

            tab.interactable = !open;
            var color = open ? _openTab : _closedTab;

            if (tab.targetGraphic != null)
            {
                tab.targetGraphic.color = color;
            }

            if (label != null)
            {
                label.color = color;
            }
        }

        private static void Label(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        private void OnEnable()
        {
            _eventBus.Subscribe<WorkbenchWindowRequestedEvent>(OnWindowRequested);
        }

        private void OnDisable()
        {
            _eventBus.Unsubscribe<WorkbenchWindowRequestedEvent>(OnWindowRequested);
            ReleasePauseIfOpen();
        }

        private void OnWindowRequested(WorkbenchWindowRequestedEvent evt)
        {
            _panel.SetActive(true);
            _showingUpgrades = false;
            UpdateTabView();

            if (!_isOpen)
            {
                _isOpen = true;
                _pauseService.RequestPause();
            }

            Refresh();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (_isOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        private void Close()
        {
            _panel.SetActive(false);
            ReleasePauseIfOpen();
        }

        private void ReleasePauseIfOpen()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            _pauseService.ReleasePause();
        }

        private void Refresh()
        {
            ClearRows();
            BuildRecipeRows();
            BuildUpgradeRows();
        }

        private void BuildRecipeRows()
        {
            var any = false;

            foreach (var recipe in _recipeService.Known)
            {
                if (recipe == null || recipe.Output == null)
                {
                    continue;
                }

                any = true;
                var captured = recipe;

                NewRow(_recipeRowPrefab, _recipesContainer).Set(
                    recipe.Output.Icon,
                    Title(recipe.Output),
                    Costs(recipe),
                    _craftingService.CanCraft(recipe),
                    () => Craft(captured));
            }

            if (!any)
            {
                NewRow(_recipeRowPrefab, _recipesContainer)
                    .SetPlaceholder("No schematics yet — traders sell the basic ones");
            }
        }

        private void BuildUpgradeRows()
        {
            var any = false;

            foreach (var pair in _itemService.All)
            {
                if (pair.Value <= 0 || pair.Key is not UpgradeItemDefinition upgrade)
                {
                    continue;
                }

                any = true;
                var captured = upgrade;

                NewUpgradeRow().Set(
                    upgrade.Icon,
                    Title(upgrade),
                    $"<color={Held}>in the hold</color>",
                    true,
                    () => Install(captured));
            }

            foreach (var upgrade in _upgradeService.Installed)
            {
                any = true;

                NewUpgradeRow().Set(
                    upgrade.Icon,
                    Title(upgrade),
                    $"<color={Enough}>installed</color>",
                    false,
                    null);
            }

            if (!any)
            {
                NewRow(_recipeRowPrefab, _upgradesContainer)
                    .SetPlaceholder("Nothing to fit — build a part on the Recipes tab first");
            }
        }

        private WorkbenchRecipeRowUI NewRow(GameObject prefab, Transform container)
        {
            var row = Instantiate(prefab, container);
            _rows.Add(row);
            return row.GetComponent<WorkbenchRecipeRowUI>();
        }

        private WorkbenchUpgradeRowUI NewUpgradeRow()
        {
            var row = Instantiate(_upgradeRowPrefab, _upgradesContainer);
            _rows.Add(row);
            return row.GetComponent<WorkbenchUpgradeRowUI>();
        }

        // Название плюс то, что предмет даёт: иначе список — это набор слов,
        // по которому нельзя решить, стоит ли тратить лом.
        private string Title(ItemDefinition item)
        {
            _titleBuilder.Clear();
            _titleBuilder.Append(item.DisplayName);

            var detail = item is UpgradeItemDefinition upgrade ? UpgradeEffectText.Describe(upgrade) : string.Empty;

            if (string.IsNullOrEmpty(detail))
            {
                detail = item.Description;
            }

            if (!string.IsNullOrEmpty(detail))
            {
                _titleBuilder.Append($"\n<size=70%><color={Detail}>{detail}</color></size>");
            }

            return _titleBuilder.ToString();
        }

        // Чего не хватает, видно по цвету: серая кнопка сама этого не объясняет.
        private string Costs(CraftingRecipe recipe)
        {
            _costsBuilder.Clear();

            foreach (var cost in recipe.Costs)
            {
                if (_costsBuilder.Length > 0)
                {
                    _costsBuilder.Append(", \n");
                }

                var have = _resourceService.GetAmount(cost.Type);
                var color = have >= cost.Amount ? Enough : Short;
                _costsBuilder.Append($"<color={color}>{cost.Type} {have}/{cost.Amount}</color>");
            }

            return _costsBuilder.ToString();
        }

        private void Craft(CraftingRecipe recipe)
        {
            _craftingService.TryCraft(recipe);
            Refresh();
        }

        private void Install(UpgradeItemDefinition upgrade)
        {
            _upgradeService.TryInstall(upgrade);
            Refresh();
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
            {
                Destroy(row);
            }

            _rows.Clear();
        }
    }
}
