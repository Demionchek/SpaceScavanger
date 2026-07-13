using System.Text;
using DG.Tweening;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public sealed class QuestHudUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _questsText;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private TMP_Text _toggleButtonLabel;
        [SerializeField] private bool _collapseToRight = true;
        [SerializeField] private float _slideDuration = 0.25f;
        [SerializeField] private Ease _slideEase = Ease.OutCubic;

        private readonly StringBuilder _builder = new();

        private IQuestService _questService;
        private Vector2 _expandedPosition;
        private Tween _slideTween;
        private bool _collapsed;

        [Inject]
        public void Construct(IQuestService questService)
        {
            _questService = questService;
        }

        private void Awake()
        {
            _expandedPosition = _panel.anchoredPosition;
            _toggleButton.onClick.AddListener(Toggle);
            UpdateToggleLabel();
            Toggle();
        }

        private void OnDestroy()
        {
            _slideTween?.Kill();
        }

        private void Toggle()
        {
            _collapsed = !_collapsed;
            UpdateToggleLabel();

            var collapsedOffset = _panel.rect.width * (_collapseToRight ? 1f : -1f);
            var target = _collapsed
                ? _expandedPosition + new Vector2(collapsedOffset, 0f)
                : _expandedPosition;

            _slideTween?.Kill();
            _slideTween = DOTween.To(
                    () => _panel.anchoredPosition,
                    value => _panel.anchoredPosition = value,
                    target,
                    _slideDuration)
                .SetEase(_slideEase)
                .SetUpdate(true);
        }

        private void UpdateToggleLabel()
        {
            if (_toggleButtonLabel == null)
            {
                return;
            }

            var expandArrow = _collapseToRight ? "<" : ">";
            var collapseArrow = _collapseToRight ? ">" : "<";
            _toggleButtonLabel.text = _collapsed ? expandArrow : collapseArrow;
        }

        private void LateUpdate()
        {
            _builder.Clear();

            foreach (var quest in _questService.ActiveQuests)
            {
                if (quest.Goal.IsComplete)
                {
                    _builder.AppendLine($"{quest.Definition.Title} — готово, сдай заказчику");
                }
                else
                {
                    _builder.AppendLine($"{quest.Definition.Title} — {Mathf.RoundToInt(quest.Goal.Progress * 100f)}%");
                }
            }

            _questsText.text = _builder.ToString();
        }
    }
}
