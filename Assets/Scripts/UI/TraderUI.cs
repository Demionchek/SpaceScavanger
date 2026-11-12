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
            EnsureScroll();
            _panel.SetActive(false);
        }

        // Список у торговца длиннее окна — схемы одни чего стоят, — а в префабе он
        // просто лежит в панели и уходит за край. Прокрутка собирается кодом, чтобы
        // окно не пришлось перекладывать руками.
        private void EnsureScroll()
        {
            if (_offersContainer is not RectTransform content
                || content.GetComponentInParent<ScrollRect>() != null
                || content.parent is not RectTransform parent)
            {
                return;
            }

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.SetParent(parent, false);

            // На месте контейнера в иерархии, иначе список ляжет поверх кнопки закрытия.
            viewportRect.SetSiblingIndex(content.GetSiblingIndex());
            viewportRect.anchorMin = content.anchorMin;
            viewportRect.anchorMax = content.anchorMax;
            viewportRect.pivot = content.pivot;
            viewportRect.anchoredPosition = content.anchoredPosition;
            viewportRect.sizeDelta = content.sizeDelta;

            content.SetParent(viewportRect, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            // Без фиттера высота контента остаётся нулевой и прокручивать нечего.
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Прозрачная подложка ловит колесо и над пустым местом списка: без
            // графики ScrollRect получает события только поверх самих строк.
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            var scroll = viewport.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 60f;
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
            if (!_tradeService.BuyRecipe(offer.Recipe, offer.Price))
            {
                WarnShortOnCredits(offer.Price);
            }

            Refresh();
        }

        // Кнопка, которая молча ничего не делает, читается как сломанная.
        private void WarnShortOnCredits(int price)
        {
            _eventBus.Publish(new WarningMessageEvent(
                $"Not enough credits: {price} needed, {_resourceService.GetAmount(ResourceType.Credits)} aboard"));
        }

        private void WarnNothingToSell(string what)
        {
            _eventBus.Publish(new WarningMessageEvent($"Not enough {what} in the hold to sell"));
        }

        private void AddRow(Sprite icon, string name, int owned, int buyPrice, int sellPrice, Action<int> onBuy, Action<int> onSell)
        {
            var row = Instantiate(_offerRowPrefab, _offersContainer);
            _rows.Add(row);
            row.GetComponent<TradeOfferRowUI>().Set(icon, name, owned, PriceLabel(buyPrice, sellPrice),
                onBuy, onSell);
        }

        // Антиматерия продаётся в одну сторону, и "Sell 0" в такой строке читалось бы
        // как поломка, а не как отказ торговца её выкупать. Рецепты так и печатались.
        private static string PriceLabel(int buyPrice, int sellPrice)
        {
            if (buyPrice <= 0)
            {
                return $"Sell {sellPrice}";
            }

            return sellPrice > 0 ? $"Buy {buyPrice} / Sell {sellPrice}" : $"Buy {buyPrice}";
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
            if (!_tradeService.BuyResource(offer.Resource, offer.BuyPrice, quantity))
            {
                WarnShortOnCredits(offer.BuyPrice * quantity);
            }

            Refresh();
        }

        private void TrySellResource(ResourceTradeOffer offer, int quantity)
        {
            if (!_tradeService.SellResource(offer.Resource, offer.SellPrice, quantity))
            {
                WarnNothingToSell(offer.Resource.ToString());
            }

            Refresh();
        }

        private void TryBuyItem(ItemTradeOffer offer, int quantity)
        {
            if (!_tradeService.BuyItem(offer.Item, offer.BuyPrice, quantity))
            {
                WarnShortOnCredits(offer.BuyPrice * quantity);
            }

            Refresh();
        }

        private void TrySellItem(ItemTradeOffer offer, int quantity)
        {
            if (!_tradeService.SellItem(offer.Item, offer.SellPrice, quantity))
            {
                WarnNothingToSell(offer.Item.DisplayName);
            }

            Refresh();
        }
    }
}
