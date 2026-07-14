using System.Collections.Generic;
using System.Text;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public sealed class WorkbenchUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _recipesContainer;
        [SerializeField] private GameObject _recipeRowPrefab;
        [SerializeField] private Transform _upgradesContainer;
        [SerializeField] private GameObject _upgradeRowPrefab;
        [SerializeField] private Button _closeButton;

        private readonly List<GameObject> _rows = new();
        private readonly StringBuilder _costsBuilder = new();

        private EventBus _eventBus;
        private ICraftingService _craftingService;
        private IUpgradeService _upgradeService;
        private IResourceService _resourceService;
        private IItemService _itemService;
        private IPauseService _pauseService;
        private CraftingRecipe[] _currentRecipes;
        private bool _isOpen;

        [Inject]
        public void Construct(
            EventBus eventBus,
            ICraftingService craftingService,
            IUpgradeService upgradeService,
            IResourceService resourceService,
            IItemService itemService,
            IPauseService pauseService)
        {
            _eventBus = eventBus;
            _craftingService = craftingService;
            _upgradeService = upgradeService;
            _resourceService = resourceService;
            _itemService = itemService;
            _pauseService = pauseService;
        }

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
            _panel.SetActive(false);
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
            _currentRecipes = evt.Recipes;
            _panel.SetActive(true);

            if (!_isOpen)
            {
                _isOpen = true;
                _pauseService.RequestPause();
            }

            Refresh();
        }

        private void Close()
        {
            _currentRecipes = null;
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
            if (_currentRecipes == null)
            {
                return;
            }

            foreach (var recipe in _currentRecipes)
            {
                if (recipe == null || recipe.Output == null)
                {
                    continue;
                }

                var row = Instantiate(_recipeRowPrefab, _recipesContainer);
                _rows.Add(row);

                var captured = recipe;
                row.GetComponent<WorkbenchRecipeRowUI>().Set(
                    recipe.Output.Icon,
                    recipe.Output.DisplayName,
                    BuildCostsText(recipe),
                    _craftingService.CanCraft(recipe),
                    () => Craft(captured));
            }
        }

        private void BuildUpgradeRows()
        {
            foreach (var pair in _itemService.All)
            {
                if (pair.Value <= 0 || pair.Key is not UpgradeItemDefinition upgrade)
                {
                    continue;
                }

                var row = Instantiate(_upgradeRowPrefab, _upgradesContainer);
                _rows.Add(row);

                row.GetComponent<WorkbenchUpgradeRowUI>().Set(
                    upgrade.Icon,
                    upgrade.DisplayName,
                    pair.Value,
                    () => Install(upgrade));
            }
        }

        private string BuildCostsText(CraftingRecipe recipe)
        {
            _costsBuilder.Clear();

            foreach (var cost in recipe.Costs)
            {
                if (_costsBuilder.Length > 0)
                {
                    _costsBuilder.Append(", ");
                }

                _costsBuilder.Append($"{cost.Type} {_resourceService.GetAmount(cost.Type)}/{cost.Amount}");
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
