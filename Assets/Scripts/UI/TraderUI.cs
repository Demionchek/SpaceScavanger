using System;
using System.Collections.Generic;
using Game.Core;
using Game.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public sealed class TraderUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _offersContainer;
        [SerializeField] private GameObject _offerRowPrefab;
        [SerializeField] private TMP_Text _creditsText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private ResourceIconSet _resourceIconSet;

        private readonly List<GameObject> _rows = new();

        private EventBus _eventBus;
        private ITradeService _tradeService;
        private IResourceService _resourceService;
        private IItemService _itemService;
        private IRecipeService _recipeService;
        private IPauseService _pauseService;
        private TraderInventory _currentInventory;
        private bool _isOpen;

        [Inject]
        public void Construct(
            EventBus eventBus,
            ITradeService tradeService,
            IResourceService resourceService,
            IItemService itemService,
            IRecipeService recipeService,
            IPauseService pauseService)
        {
            _eventBus = eventBus;
            _tradeService = tradeService;
            _resourceService = resourceService;
            _itemService = itemService;
            _recipeService = recipeService;
            _pauseService = pauseService;
        }

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            _eventBus.Subscribe<TradeWindowRequestedEvent>(OnTradeWindowRequested);
        }

        private void OnDisable()
        {
            _eventBus.Unsubscribe<TradeWindowRequestedEvent>(OnTradeWindowRequested);
            ReleasePauseIfOpen();
        }

        private void OnTradeWindowRequested(TradeWindowRequestedEvent evt)
        {
            _currentInventory = evt.Inventory;
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
            _currentInventory = null;
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

            _creditsText.text = $"Credits: {_resourceService.GetAmount(ResourceType.Credits)}";

            if (_currentInventory == null)
            {
                return;
            }

            foreach (var offer in _currentInventory.ResourceOffers)
            {
                AddRow(
                    _resourceIconSet != null ? _resourceIconSet.GetIcon(offer.Resource) : null,
                    offer.Resource.ToString(),
                    _resourceService.GetAmount(offer.Resource),
                    offer.BuyPrice,
                    offer.SellPrice,
                    offer.BuyPrice > 0 ? (Action<int>)(quantity => TryBuyResource(offer, quantity)) : null,
                    offer.SellPrice > 0 ? (Action<int>)(quantity => TrySellResource(offer, quantity)) : null);
            }

            foreach (var offer in _currentInventory.ItemOffers)
            {
                if (offer.Item == null)
                {
                    continue;
                }

                AddRow(
                    offer.Item.Icon,
                    offer.Item.DisplayName,
                    _itemService.GetAmount(offer.Item),
                    offer.BuyPrice,
                    offer.SellPrice,
                    offer.BuyPrice > 0 ? (Action<int>)(quantity => TryBuyItem(offer, quantity)) : null,
                    offer.SellPrice > 0 ? (Action<int>)(quantity => TrySellItem(offer, quantity)) : null);
            }

            if (_currentInventory.RecipeOffers == null)
            {
                return;
            }

            foreach (var offer in _currentInventory.RecipeOffers)
            {
                if (offer.Recipe == null || offer.Recipe.Output == null || _recipeService.Knows(offer.Recipe))
                {
                    continue;
                }

                var captured = offer;
                var row = Instantiate(_offerRowPrefab, _offersContainer);
                _rows.Add(row);
                row.GetComponent<TradeOfferRowUI>().Set(
                    captured.Recipe.Output.Icon,
                    $"Recipe: {captured.Recipe.Output.DisplayName}",
                    0,
                    $"Buy {captured.Price}",
                    _ => TryBuyRecipe(captured),
                    null);
            }
        }

        private void TryBuyRecipe(RecipeTradeOffer offer)
        {
            _tradeService.BuyRecipe(offer.Recipe, offer.Price);
            Refresh();
        }

        private void AddRow(Sprite icon, string name, int owned, int buyPrice, int sellPrice, Action<int> onBuy, Action<int> onSell)
        {
            var row = Instantiate(_offerRowPrefab, _offersContainer);
            _rows.Add(row);
            row.GetComponent<TradeOfferRowUI>().Set(icon, name, owned, $"Buy {buyPrice} / Sell {sellPrice}", onBuy, onSell);
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
            {
                Destroy(row);
            }

            _rows.Clear();
        }

        private void TryBuyResource(ResourceTradeOffer offer, int quantity)
        {
            _tradeService.BuyResource(offer.Resource, offer.BuyPrice, quantity);
            Refresh();
        }

        private void TrySellResource(ResourceTradeOffer offer, int quantity)
        {
            _tradeService.SellResource(offer.Resource, offer.SellPrice, quantity);
            Refresh();
        }

        private void TryBuyItem(ItemTradeOffer offer, int quantity)
        {
            _tradeService.BuyItem(offer.Item, offer.BuyPrice, quantity);
            Refresh();
        }

        private void TrySellItem(ItemTradeOffer offer, int quantity)
        {
            _tradeService.SellItem(offer.Item, offer.SellPrice, quantity);
            Refresh();
        }
    }
}
