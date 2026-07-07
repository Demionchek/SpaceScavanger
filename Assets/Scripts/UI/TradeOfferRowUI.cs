using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class TradeOfferRowUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _ownedText;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private TMP_InputField _quantityInput;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _sellButton;

        public void Set(Sprite icon, string name, int owned, string priceLabel, Action<int> onBuy, Action<int> onSell)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _nameText.text = name;
            _ownedText.text = owned.ToString();
            _priceText.text = priceLabel;
            _quantityInput.text = "1";

            _buyButton.onClick.RemoveAllListeners();
            _sellButton.onClick.RemoveAllListeners();

            _buyButton.gameObject.SetActive(onBuy != null);
            if (onBuy != null)
            {
                _buyButton.onClick.AddListener(() => onBuy(GetQuantity()));
            }

            _sellButton.gameObject.SetActive(onSell != null);
            if (onSell != null)
            {
                _sellButton.onClick.AddListener(() => onSell(GetQuantity()));
            }
        }

        private int GetQuantity()
        {
            return int.TryParse(_quantityInput.text, out var quantity) && quantity > 0 ? quantity : 1;
        }
    }
}
