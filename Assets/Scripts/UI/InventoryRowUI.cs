using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class InventoryRowUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _amountText;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionLabel;

        public void Set(Sprite icon, string name, string amount, string actionLabel, Action onAction)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _nameText.text = name;
            _amountText.text = amount;

            if (_actionButton == null)
            {
                return;
            }

            _actionButton.onClick.RemoveAllListeners();
            _actionButton.gameObject.SetActive(onAction != null);

            if (onAction == null)
            {
                return;
            }

            _actionButton.onClick.AddListener(() => onAction());

            if (_actionLabel != null)
            {
                _actionLabel.text = actionLabel;
            }
        }
    }
}
