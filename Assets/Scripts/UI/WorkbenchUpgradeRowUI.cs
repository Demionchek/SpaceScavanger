using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class WorkbenchUpgradeRowUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _ownedText;
        [SerializeField] private Button _installButton;

        public void Set(Sprite icon, string title, string status, bool canInstall, Action onInstall)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _nameText.text = title;
            _ownedText.text = status;

            _installButton.onClick.RemoveAllListeners();

            // Установленное улучшение остаётся в списке, но уже без кнопки:
            // иначе игрок не видит, что именно у него стоит.
            _installButton.gameObject.SetActive(canInstall);

            if (canInstall)
            {
                _installButton.interactable = true;
                _installButton.onClick.AddListener(() => onInstall());
            }
        }
    }
}
