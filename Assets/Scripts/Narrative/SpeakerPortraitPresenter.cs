using Game.Data;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace Game.Narrative
{
    // Показывает иконку говорящего рядом с репликой. Добавляется в список
    // Dialogue Presenters у DialogueRunner рядом с SpeakerTalkPresenter.
    public sealed class SpeakerPortraitPresenter : DialoguePresenterBase
    {
        [SerializeField] private SpeakerIconSet _icons;
        [Tooltip("Куда подставляется иконка.")]
        [SerializeField] private Image _target;
        [Tooltip("Прячется целиком, когда у говорящего иконки нет. Пусто — прячем сам Image.")]
        [SerializeField] private GameObject _root;

        [Header("Окно диалога")]
        [Tooltip("Раскладка окна: её левый отступ освобождает место под портрет.")]
        [SerializeField] private VerticalLayoutGroup _barLayout;
        [Tooltip("Левый отступ с портретом и без него.")]
        [SerializeField] private int _paddingLeftWithPortrait = 340;
        [SerializeField] private int _paddingLeftPlain = 40;
        [Tooltip("LayoutElement текста: через него окно растягивается по высоте портрета.")]
        [SerializeField] private LayoutElement _barHeight;
        [Tooltip("Минимальная высота текста с портретом и без него; -1 — без ограничения.")]
        [SerializeField] private float _minHeightWithPortrait = 240f;
        [SerializeField] private float _minHeightPlain = -1f;
        [Tooltip("Раскладка списка вариантов ответа: у неё та же геометрия, что и у окна, " +
                 "поэтому отступ нужен тот же — иначе ответы уезжают под портрет.")]
        [SerializeField] private LayoutGroup _optionsLayout;
        [Tooltip("LayoutElement панели вариантов: держит ей ту же минимальную высоту, " +
                 "что и у окна реплики, иначе панель меняет размер на выборе ответа.")]
        [SerializeField] private LayoutElement _optionsHeight;
        [Tooltip("Минимальная высота панели вариантов с портретом: высота текста плюс " +
                 "отступы раскладки сверху и снизу.")]
        [SerializeField] private float _optionsMinHeightWithPortrait = 296f;
        [SerializeField] private float _optionsMinHeightPlain = -1f;

        // Иконка живёт только во время диалога: страхуемся от того, что объект
        // оставили включённым в сцене — иначе до первой реплики висит пустой квадрат.
        private void Awake()
        {
            Show(null);
        }

        public override YarnTask OnDialogueStartedAsync()
        {
            Show(null);
            return YarnTask.CompletedTask;
        }

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            Show(_icons != null ? _icons.GetIcon(line.CharacterName) : null);
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            Show(null);
            return YarnTask.CompletedTask;
        }

        private void Show(Sprite icon)
        {
            if (_target != null)
            {
                _target.sprite = icon;
            }

            var holder = _root != null ? _root : (_target != null ? _target.gameObject : null);
            if (holder != null)
            {
                holder.SetActive(icon != null);
            }

            ApplyBarLayout(icon != null);
        }

        // Оба размера задаются здесь обоими значениями, а не снимаются со сцены:
        // иначе окно навсегда запомнит то, что лежало в ней на момент запуска.
        private void ApplyBarLayout(bool hasIcon)
        {
            var left = hasIcon ? _paddingLeftWithPortrait : _paddingLeftPlain;
            ApplyPaddingLeft(_barLayout, left);
            ApplyPaddingLeft(_optionsLayout, left);

            if (_barHeight != null)
            {
                _barHeight.minHeight = hasIcon ? _minHeightWithPortrait : _minHeightPlain;
            }

            if (_optionsHeight != null)
            {
                _optionsHeight.minHeight = hasIcon ? _optionsMinHeightWithPortrait : _optionsMinHeightPlain;
            }
        }

        private static void ApplyPaddingLeft(LayoutGroup layout, int left)
        {
            if (layout == null || layout.padding.left == left)
            {
                return;
            }

            // Правка полей RectOffset на месте не помечает раскладку грязной.
            var padding = layout.padding;
            layout.padding = new RectOffset(left, padding.right, padding.top, padding.bottom);
        }
    }
}
