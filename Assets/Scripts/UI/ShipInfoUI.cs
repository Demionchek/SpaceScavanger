using System.Text;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public sealed class ShipInfoUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _statsText;
        [SerializeField] private TMP_Text _upgradesText;
        [SerializeField] private Button _closeButton;

        private readonly StringBuilder _builder = new();

        private EventBus _eventBus;
        private IShipStatsService _statsService;
        private IUpgradeService _upgradeService;
        private IPauseService _pauseService;
        private bool _isOpen;

        [Inject]
        public void Construct(
            EventBus eventBus,
            IShipStatsService statsService,
            IUpgradeService upgradeService,
            IPauseService pauseService)
        {
            _eventBus = eventBus;
            _statsService = statsService;
            _upgradeService = upgradeService;
            _pauseService = pauseService;
        }

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            _eventBus.Subscribe<ShipInfoWindowRequestedEvent>(OnWindowRequested);
        }

        private void OnDisable()
        {
            _eventBus.Unsubscribe<ShipInfoWindowRequestedEvent>(OnWindowRequested);
            ReleasePauseIfOpen();
        }

        private void OnWindowRequested(ShipInfoWindowRequestedEvent evt)
        {
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
            _builder.Clear();
            AppendStat("Тяга", ShipStat.ThrustForce);
            AppendStat("Урон", ShipStat.Damage);
            AppendStat("Скорострельность", ShipStat.FireRate);
            AppendStat("Крюк", ShipStat.HookLevel);
            AppendStat("Прочность", ShipStat.MaxHealth);
            _statsText.text = _builder.ToString();

            _builder.Clear();
            _builder.AppendLine("Установленные апгрейды:");

            if (_upgradeService.Installed.Count == 0)
            {
                _builder.AppendLine("— нет —");
            }
            else
            {
                foreach (var upgrade in _upgradeService.Installed)
                {
                    _builder.AppendLine($"• {upgrade.DisplayName}");
                }
            }

            _upgradesText.text = _builder.ToString();
        }

        private void AppendStat(string label, ShipStat stat)
        {
            var multiplier = _statsService.GetMultiplier(stat);
            var bonus = _statsService.GetBonus(stat);

            _builder.Append($"{label}: x{multiplier:0.##}");

            if (bonus != 0)
            {
                _builder.Append($" +{bonus}");
            }

            _builder.AppendLine();
        }
    }
}
