using System;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Game.UI
{
    // Ползунок бегает по полосе, попасть надо в зелёный отрезок. Окно держит паузу,
    // поэтому время тут нескалированное, а клавиатура опрашивается напрямую: карты
    // ввода на паузе выключены.
    public sealed class TimingPuzzleUI : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _font;

        [Header("Оформление")]
        [SerializeField] private Vector2 _panelSize = new(1600f, 1000f);

        [Tooltip("Отступ содержимого от края панели: у рамы широкая кромка.")]
        [SerializeField] private Vector2 _frameInset = new(120f, 200f);

        [SerializeField] private Vector2 _trackSize = new(1200f, 140f);

        [Tooltip("Насколько зелёная зона ниже самой полосы, чтобы кромка поля осталась видна.")]
        [SerializeField] private float _bandInset = 46f;

        [SerializeField] private float _markerWidth = 20f;
        [SerializeField] private float _markerOverhang = 35f;

        [Tooltip("Клавиша, которой ловят ползунок. Карты ввода на паузе выключены, " +
                 "поэтому она задана здесь, а не привязкой.")]
        [SerializeField] private Key _actionKey = Key.E;

        [SerializeField] private Color _backdrop = new(0f, 0f, 0f, 0.72f);
        [SerializeField] private Color _frameTint = Color.white;

        [Tooltip("Чем залить плиту, если спрайт рамы не задан: белым окно читается как поломка.")]
        [SerializeField] private Color _plateFallback = new(0.05f, 0.06f, 0.09f, 0.97f);
        [SerializeField] private Color _band = new(0.33f, 0.85f, 0.4f, 1f);
        [SerializeField] private Color _marker = Color.white;
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _dimColor = new(0.65f, 0.7f, 0.78f, 1f);

        private EventBus _eventBus;
        private IPauseService _pauseService;
        private WindowSkin _skin;
        private PuzzlePanel _panel;
        private RectTransform _bandRect;
        private RectTransform _markerRect;
        private Action<bool> _onFinished;

        private bool _open;
        private int _openedFrame;
        private int _rounds;
        private int _hits;
        private float _bandWidth;
        private float _bandCenter;
        private float _speed;
        private float _position;
        private float _direction = 1f;
        private float _time;

        [Inject]
        public void Construct(EventBus eventBus, IPauseService pauseService, WindowSkin skin)
        {
            _eventBus = eventBus;
            _pauseService = pauseService;
            _skin = skin;
            _eventBus.Subscribe<TimingPuzzleRequestedEvent>(OnRequested);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<TimingPuzzleRequestedEvent>(OnRequested);
        }

        private void OnRequested(TimingPuzzleRequestedEvent evt)
        {
            if (_open)
            {
                return;
            }

            Build();

            _open = true;
            _openedFrame = Time.frameCount;
            _onFinished = evt.OnFinished;
            _rounds = Mathf.Max(1, evt.Rounds);
            _hits = 0;
            _bandWidth = Mathf.Clamp(evt.BandWidth, 0.04f, 0.9f);
            _speed = Mathf.Max(0.1f, evt.Speed);
            _time = Mathf.Max(1f, evt.Seconds);
            _position = 0f;
            _direction = 1f;

            NextBand();
            _panel.Open(evt.Title, $"Press {_actionKey} while the marker is inside the green band.");
            Paint();
            _pauseService.RequestPause();
        }

        private void Update()
        {
            if (!_open)
            {
                return;
            }

            _time -= Time.unscaledDeltaTime;
            Move();
            Paint();

            if (_time <= 0f)
            {
                Close(false);
                return;
            }

            // Кадр открытия пропускаем: клавиша, которой игрок закрыл разговор, в
            // этом же кадре ещё считается нажатой.
            var keyboard = Keyboard.current;
            if (keyboard == null || Time.frameCount == _openedFrame)
            {
                return;
            }

            if (keyboard[_actionKey].wasPressedThisFrame)
            {
                Press();
            }
        }

        private void Move()
        {
            _position += _direction * _speed * Time.unscaledDeltaTime;

            if (_position > 1f)
            {
                _position = 1f;
                _direction = -1f;
            }
            else if (_position < 0f)
            {
                _position = 0f;
                _direction = 1f;
            }
        }

        // Промах заканчивает всё сразу: ставка на одно нажатие и есть вся игра.
        private void Press()
        {
            if (Mathf.Abs(_position - _bandCenter) > _bandWidth * 0.5f)
            {
                Close(false);
                return;
            }

            _hits++;
            if (_hits >= _rounds)
            {
                Close(true);
                return;
            }

            NextBand();
        }

        private void NextBand()
        {
            var half = _bandWidth * 0.5f;
            _bandCenter = UnityEngine.Random.Range(half, 1f - half);
        }

        private void Paint()
        {
            var height = Mathf.Max(10f, _trackSize.y - _bandInset);
            _bandRect.sizeDelta = new Vector2(_trackSize.x * _bandWidth, height);
            _bandRect.anchoredPosition = new Vector2((_bandCenter - 0.5f) * _trackSize.x, 0f);
            _markerRect.anchoredPosition = new Vector2((_position - 0.5f) * _trackSize.x, 0f);
            _panel.SetStatus($"Lock {_hits + 1} / {_rounds}   —   {Mathf.Max(0f, _time):0.0} s");
        }

        // Пауза снимается ДО колбэка: он ставит взрыв на корпус, а тот должен
        // доиграть в уже идущем времени.
        private void Close(bool success)
        {
            _open = false;
            _panel.Close();

            var finished = _onFinished;
            _onFinished = null;
            _pauseService.ReleasePause();
            finished?.Invoke(success);
        }

        private void Build()
        {
            if (_panel != null)
            {
                return;
            }

            _panel = new PuzzlePanel(transform, new PuzzlePanel.Style
            {
                Font = _font,
                PanelSize = _panelSize,
                FrameInset = _frameInset,
                Frame = _skin?.Frame,
                FramePixelsPerUnit = _skin?.PixelsPerUnit ?? 1f,
                Backdrop = _backdrop,
                FrameTint = _frameTint,
                PlateFallback = _plateFallback,
                Text = _textColor,
                Dim = _dimColor
            });

            var track = PuzzlePanel.Box("Track", _panel.Content, _trackSize, Vector2.zero);
            PuzzlePanel.Plate(track, _skin?.Field, _skin?.PixelsPerUnit ?? 1f, _frameTint,
                _plateFallback);

            _bandRect = PuzzlePanel.Box("Band", track, _trackSize, Vector2.zero);
            PuzzlePanel.Plate(_bandRect, _band);

            _markerRect = PuzzlePanel.Box("Marker", track,
                new Vector2(_markerWidth, _trackSize.y + _markerOverhang * 2f), Vector2.zero);
            PuzzlePanel.Plate(_markerRect, _marker);
        }
    }
}
