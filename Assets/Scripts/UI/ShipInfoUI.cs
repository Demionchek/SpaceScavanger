using System.Globalization;
using System.Text;
using Game.Core;
using Game.Gameplay.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    // Экран состояния корабля. В рамку справа вшита колонка из четырёх ячеек, и
    // строки встают ровно против них: иконка каждой строки кладётся поверх своей
    // ячейки, поэтому ряды собираются кодом по координатам, а не одним текстовым
    // блоком. Что в рамку не влезло — слева, со своими иконками того же набора.
    public sealed class ShipInfoUI : MonoBehaviour
    {
        private const string Good = "#8CE99A";
        private const string Warn = "#FFD43B";
        private const string Dim = "#CED4DA";
        private const string Faint = "#7A818A";

        private static readonly ReadoutRow[] Readout =
        {
            new("Damage", ShipStat.Damage, "{0:0.#}"),
            new("Hull", ShipStat.MaxHealth, null),
            new("Fire rate", ShipStat.FireRate, "{0:0.##}/s"),
            new("Thrust", ShipStat.ThrustForce, "{0:0.#}")
        };

        // Каталог идёт семействами в этом порядке: крюк первым — он решает, какие
        // ресурсы вообще доступны.
        private static readonly ShipStat[] Families =
        {
            ShipStat.HookLevel, ShipStat.Damage, ShipStat.FireRate,
            ShipStat.MaxHealth, ShipStat.ThrustForce, ShipStat.RadarRange
        };

        private static readonly ReadoutRow[] LeftStats =
        {
            new("Hook level", ShipStat.HookLevel, "{0:0}"),
            new("Radar", ShipStat.RadarRange, "{0:0.#} u")
        };

        [SerializeField] private GameObject _panel;

        [Tooltip("Заголовок левой колонки; у него же берётся шрифт для строк.")]
        [SerializeField] private TMP_Text _statsText;

        [SerializeField] private TMP_Text _upgradesText;
        [SerializeField] private Button _closeButton;

        [Header("Иконки строк рамки — сверху вниз")]
        [SerializeField] private Sprite _damageIcon;

        [SerializeField] private Sprite _hullIcon;
        [SerializeField] private Sprite _fireRateIcon;
        [SerializeField] private Sprite _thrustIcon;

        [Header("Иконки левой колонки")]
        [SerializeField] private Sprite _hookIcon;

        [SerializeField] private Sprite _radarIcon;

        [Header("Общее для строк")]
        [SerializeField] private float _rowSpacing = 76.7f;

        [SerializeField] private float _rowHeight = 70f;
        [SerializeField] private float _rowFontSize = 40f;

        [Tooltip("Чуть больше ячейки в рамке, чтобы вшитая иконка не выглядывала краем.")]
        [SerializeField] private float _iconSize = 78f;

        [Header("Правый экран: строки против ячеек рамки")]
        [SerializeField] private float _readoutIconX = 368f;

        [SerializeField] private float _readoutTextX = 668f;
        [SerializeField] private float _readoutWidth = 500f;
        [SerializeField] private float _readoutFirstRowY = 291f;

        [Header("Левая колонка")]
        [SerializeField] private float _leftIconX = -845f;

        [SerializeField] private float _leftTextX = -465f;
        [SerializeField] private float _leftWidth = 640f;
        [SerializeField] private float _leftFirstRowY = 270f;

        [Header("Корпус и ремонт (блок собирается кодом)")]
        [SerializeField] private Vector2 _hullBlockPosition = new(-500f, -320f);

        [SerializeField] private Vector2 _hullBlockSize = new(760f, 190f);
        [SerializeField] private float _hullFontSize = 34f;
        [SerializeField] private float _hullBarHeight = 26f;
        [SerializeField] private Vector2 _repairButtonSize = new(520f, 100f);

        [Tooltip("Тот же спрайт, что у вкладок верстака.")]
        [SerializeField] private Sprite _repairButtonSprite;

        private readonly StringBuilder _builder = new();

        private EventBus _eventBus;
        private IShipStatsService _statsService;
        private IUpgradeService _upgradeService;
        private IPauseService _pauseService;
        private IRepairService _repairService;
        private IItemService _itemService;
        private IRecipeService _recipeService;
        private SaveAssetRegistry _registry;
        private Health _health;
        private bool _isOpen;

        private TMP_Text[] _readoutValues;
        private TMP_Text[] _leftValues;
        private Image _hullFill;
        private Button _repairButton;
        private TMP_Text _repairLabel;

        [Inject]
        public void Construct(
            EventBus eventBus,
            IShipStatsService statsService,
            IUpgradeService upgradeService,
            IPauseService pauseService,
            IRepairService repairService,
            IItemService itemService,
            IRecipeService recipeService,
            SaveAssetRegistry registry,
            Health health)
        {
            _eventBus = eventBus;
            _statsService = statsService;
            _upgradeService = upgradeService;
            _pauseService = pauseService;
            _repairService = repairService;
            _itemService = itemService;
            _recipeService = recipeService;
            _registry = registry;
            _health = health;
        }

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
            _statsText.text = "<b>SYSTEMS</b>";

            _readoutValues = BuildRows(
                Readout,
                new[] { _damageIcon, _hullIcon, _fireRateIcon, _thrustIcon },
                _readoutIconX, _readoutTextX, _readoutWidth, _readoutFirstRowY);

            _leftValues = BuildRows(
                LeftStats,
                new[] { _hookIcon, _radarIcon },
                _leftIconX, _leftTextX, _leftWidth, _leftFirstRowY);

            BuildHullBlock();
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
            RefreshValues(Readout, _readoutValues);
            RefreshValues(LeftStats, _leftValues);
            _upgradesText.text = BuildUpgrades();
            RefreshHull();
        }

        private void RefreshValues(ReadoutRow[] rows, TMP_Text[] values)
        {
            for (var i = 0; i < rows.Length; i++)
            {
                values[i].text = Value(rows[i]);
            }
        }

        // Пустой формат — строка корпуса: её число берётся не из статов, а из Health.
        private string Value(ReadoutRow row)
        {
            if (row.Format == null)
            {
                return $"{Mathf.CeilToInt(_health.CurrentHealth)} / {Mathf.CeilToInt(_health.MaxHealth)}";
            }

            if (!_statsService.TryGetEffective(row.Stat, out var value))
            {
                return "—";
            }

            return string.Format(CultureInfo.InvariantCulture, row.Format, value) + Modifier(row.Stat);
        }

        private string Modifier(ShipStat stat)
        {
            var multiplier = _statsService.GetMultiplier(stat);
            var bonus = _statsService.GetBonus(stat);

            if (Mathf.Approximately(multiplier, 1f) && bonus == 0)
            {
                return string.Empty;
            }

            _builder.Clear();
            _builder.Append($" <size=75%><color={Good}>");

            if (!Mathf.Approximately(multiplier, 1f))
            {
                _builder.AppendFormat(CultureInfo.InvariantCulture, "×{0:0.##} ", multiplier);
            }

            if (bonus != 0)
            {
                _builder.AppendFormat(CultureInfo.InvariantCulture, "+{0}", bonus);
            }

            return _builder.Append("</color></size>").ToString();
        }

        // Экран показывает не только установленное, но и всё, что в игре есть:
        // пустой список из нуля строк не говорит игроку, к чему он идёт. Но ступеней
        // стало два десятка, поэтому строка — это семейство, а не предмет: сколько
        // ступеней закрыто и что со следующей. Семейство — это стат, который оно
        // двигает, у каждого свой. Весь блок обязан уложиться в семь строк: поле
        // 720x330 при кегле 36, а сразу под ним начинается корпус с кнопкой ремонта.
        private string BuildUpgrades()
        {
            _builder.Clear();
            _builder.AppendLine("<b>UPGRADES</b>");

            var any = false;

            foreach (var stat in Families)
            {
                any |= AppendFamily(stat);
            }

            if (!any)
            {
                _builder.Append($"<color={Faint}>No upgrades in the registry yet</color>");
            }

            return _builder.ToString();
        }

        // Ступени идут в порядке реестра — он же порядок, в котором они собираются.
        private bool AppendFamily(ShipStat stat)
        {
            if (_registry.Items == null)
            {
                return false;
            }

            UpgradeItemDefinition next = null;
            var levels = 0;
            var fitted = 0;

            foreach (var item in _registry.Items)
            {
                if (item is not UpgradeItemDefinition upgrade || !Drives(upgrade, stat))
                {
                    continue;
                }

                levels++;

                if (_upgradeService.IsInstalled(upgrade))
                {
                    fitted++;
                }
                else if (next == null)
                {
                    next = upgrade;
                }
            }

            if (levels == 0)
            {
                return false;
            }

            _builder.Append(UpgradeEffectText.Label(stat))
                .Append("<pos=45%>").Append(fitted).Append('/').Append(levels)
                .Append("<pos=58%><size=80%>")
                .Append(next != null ? Status(next) : $"<color={Good}>complete</color>")
                .AppendLine("</size>");

            return true;
        }

        private static bool Drives(UpgradeItemDefinition upgrade, ShipStat stat)
        {
            if (upgrade.Modifiers == null)
            {
                return false;
            }

            foreach (var modifier in upgrade.Modifiers)
            {
                if (modifier.Stat == stat)
                {
                    return true;
                }
            }

            return false;
        }

        // Коротко: в третьей колонке около 300 px, и длинная строка тут же уезжает
        // на второй ряд — а их всего семь, дальше начинается блок корпуса.
        private string Status(UpgradeItemDefinition next)
        {
            if (_itemService.GetAmount(next) > 0)
            {
                return $"<color={Warn}>in the hold</color>";
            }

            return HasSchematic(next)
                ? $"<color={Dim}>at the bench</color>"
                : $"<color={Faint}>no schematic</color>";
        }

        private bool HasSchematic(UpgradeItemDefinition upgrade)
        {
            foreach (var recipe in _recipeService.Known)
            {
                if (recipe != null && recipe.Output == upgrade)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshHull()
        {
            var max = _health.MaxHealth;
            var percent = max > 0f ? Mathf.Clamp01(_health.CurrentHealth / max) : 0f;

            if (_hullFill != null)
            {
                _hullFill.rectTransform.anchorMax = new Vector2(percent, 1f);
                _hullFill.color = percent > 0.6f
                    ? new Color(0.55f, 0.91f, 0.6f)
                    : percent > 0.3f
                        ? new Color(1f, 0.83f, 0.23f)
                        : new Color(0.9f, 0.32f, 0.32f);
            }

            if (_repairButton != null)
            {
                _repairButton.interactable = _repairService.CanRepair;
            }

            if (_repairLabel == null)
            {
                return;
            }

            if (_repairService.MissingHull <= 0)
            {
                _repairLabel.text = "Hull intact";
                return;
            }

            // Чинится то, что можно оплатить: частичный ремонт понятнее кнопки,
            // которая просто не нажимается.
            _repairLabel.text = _repairService.CanRepair
                ? $"Repair +{_repairService.RepairableHull} — {_repairService.AffordableCost} {_repairService.Resource}"
                : $"No {_repairService.Resource} to repair with";
        }

        private void Repair()
        {
            _repairService.Repair();
            Refresh();
        }

        private TMP_Text[] BuildRows(
            ReadoutRow[] rows, Sprite[] icons, float iconX, float textX, float width, float firstY)
        {
            var values = new TMP_Text[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var y = firstY - i * _rowSpacing;
                BuildIcon(rows[i].Label, icons[i], new Vector2(iconX, y));

                var label = RowText($"{rows[i].Label} label", TextAlignmentOptions.Left, textX, y, width);
                label.text = rows[i].Label;
                values[i] = RowText($"{rows[i].Label} value", TextAlignmentOptions.Right, textX, y, width);
            }

            return values;
        }

        private TMP_Text RowText(string name, TextAlignmentOptions alignment, float x, float y, float width)
        {
            var text = NewText(name, _panel.transform, alignment, _rowFontSize);
            Place(text.rectTransform, new Vector2(x, y), new Vector2(width, _rowHeight));
            return text;
        }

        private void BuildIcon(string name, Sprite sprite, Vector2 position)
        {
            if (sprite == null)
            {
                return;
            }

            var rect = NewRect($"{name} icon", _panel.transform);
            Place(rect, position, new Vector2(_iconSize, _iconSize));

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
        }

        // Блок ставится вручную, без layout-группы: полоса и кнопка должны остаться
        // внутри левой панели окна, а её границы заданы картинкой рамки.
        private void BuildHullBlock()
        {
            var root = NewRect("HullBlock", _panel.transform);
            Place(root, _hullBlockPosition, _hullBlockSize);

            var half = _hullBlockSize.y * 0.5f;
            var width = _hullBlockSize.x;

            var hullLabel = NewText("HullLabel", root, TextAlignmentOptions.Left, _hullFontSize);
            Place(hullLabel.rectTransform, new Vector2(0f, half - 24f), new Vector2(width, 44f));
            hullLabel.text = "<b>HULL</b>";

            var bar = NewRect("HullBar", root);
            Place(bar, new Vector2(0f, half - 62f), new Vector2(width, _hullBarHeight));
            bar.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var fill = NewRect("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            _hullFill = fill.gameObject.AddComponent<Image>();

            var button = NewRect("RepairButton", root);
            Place(button, new Vector2((_repairButtonSize.x - width) * 0.5f, -half + _repairButtonSize.y * 0.5f),
                _repairButtonSize);

            var background = button.gameObject.AddComponent<Image>();
            background.sprite = _repairButtonSprite;
            background.color = _repairButtonSprite != null ? Color.white : new Color(0.16f, 0.19f, 0.24f, 0.95f);

            _repairButton = button.gameObject.AddComponent<Button>();
            _repairButton.targetGraphic = background;
            _repairButton.onClick.AddListener(Repair);

            _repairLabel = NewText("Label", button, TextAlignmentOptions.Center, _hullFontSize);
            var label = _repairLabel.rectTransform;
            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.offsetMin = Vector2.zero;
            label.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var created = new GameObject(name, typeof(RectTransform));
            created.transform.SetParent(parent, false);
            return (RectTransform)created.transform;
        }

        private TMP_Text NewText(string name, Transform parent, TextAlignmentOptions alignment, float size)
        {
            var text = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();

            if (_statsText != null)
            {
                text.font = _statsText.font;
            }

            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private readonly struct ReadoutRow
        {
            public readonly string Label;
            public readonly ShipStat Stat;
            public readonly string Format;

            public ReadoutRow(string label, ShipStat stat, string format)
            {
                Label = label;
                Stat = stat;
                Format = format;
            }
        }
    }
}
