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

        public void Set(Sprite icon, string name, int owned, Action onInstall)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _nameText.text = name;
            _ownedText.text = $"x{owned}";

            _installButton.onClick.RemoveAllListeners();
            _installButton.onClick.AddListener(() => onInstall());
        }
    }
}
