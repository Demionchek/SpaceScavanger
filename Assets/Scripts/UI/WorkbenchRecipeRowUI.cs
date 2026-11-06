using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class WorkbenchRecipeRowUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _costsText;
        [SerializeField] private Button _craftButton;

        public void Set(Sprite icon, string title, string costs, bool canCraft, Action onCraft)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _nameText.text = title;
            _costsText.text = costs;

            _craftButton.gameObject.SetActive(true);
            _craftButton.onClick.RemoveAllListeners();
            _craftButton.onClick.AddListener(() => onCraft());
            _craftButton.interactable = canCraft;
        }

        // Пустой список ничего не объясняет, поэтому вместо него — строка-подсказка.
        public void SetPlaceholder(string message)
        {
            _icon.enabled = false;
            _nameText.text = message;
            _costsText.text = string.Empty;

            _craftButton.onClick.RemoveAllListeners();
            _craftButton.gameObject.SetActive(false);
        }
    }
}
