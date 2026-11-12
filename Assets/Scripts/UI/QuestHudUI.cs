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
        [SerializeField] private bool _startCollapsed = true;
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
            Slide(_startCollapsed);
        }

        private void OnDestroy()
        {
            _slideTween?.Kill();
        }

        private void Toggle()
        {
            _collapsed = !_collapsed;
            Slide(_collapsed);
        }

        private void Slide(bool collapsed)
        {
            UpdateToggleLabel();

            var collapsedOffset = _panel.rect.width * (_collapseToRight ? 1f : -1f);
            var target = collapsed
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
                    _builder.AppendLine($"{quest.Definition.Title} — complete, turn it in");
                }
                else if (quest.Goal is IQuestGoalStatus status)
                {
                    _builder.AppendLine($"{quest.Definition.Title} — {status.Status}");
                }
                else
                {
                    _builder.AppendLine($"{quest.Definition.Title} — in progress");
                }
            }

            _questsText.text = _builder.ToString();
        }
    }
}
