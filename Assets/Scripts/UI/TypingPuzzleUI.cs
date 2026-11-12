using System;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Game.UI
{
    // Взлом: напечатать слова, пока не вышло время. Буквы читаются по клавишам, а
    // не по вводимым символам: на русской раскладке onTextInput вернул бы "ф"
    // вместо "a", и английское слово стало бы непечатаемым. Карты ввода на паузе
    // выключены, а окно паузу держит, поэтому клавиатура опрашивается напрямую.
    public sealed class TypingPuzzleUI : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _font;

        [Header("Оформление")]
        [SerializeField] private Vector2 _panelSize = new(1600f, 1000f);

        [Tooltip("Отступ содержимого от края панели: у рамы широкая кромка.")]
        [SerializeField] private Vector2 _frameInset = new(120f, 200f);

        [SerializeField] private Vector2 _wordPlateSize = new(1200f, 190f);
        [SerializeField] private float _wordFontSize = 120f;

        [SerializeField] private Color _backdrop = new(0f, 0f, 0f, 0.72f);
        [SerializeField] private Color _frameTint = Color.white;

        [Tooltip("Чем залить плиту, если спрайт рамы не задан: белым окно читается как поломка.")]
        [SerializeField] private Color _plateFallback = new(0.05f, 0.06f, 0.09f, 0.97f);
        [SerializeField] private Color _typed = new(0.33f, 0.85f, 0.4f, 1f);
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _dimColor = new(0.65f, 0.7f, 0.78f, 1f);

        private EventBus _eventBus;
        private IPauseService _pauseService;
        private WindowSkin _skin;
        private PuzzlePanel _panel;
        private TMP_Text _wordText;
        private Action<bool> _onFinished;
        private string[] _words;
        private string _typedHex;

        private bool _open;
        private int _openedFrame;
        private int _word;
        private int _letters;
        private float _time;

        [Inject]
        public void Construct(EventBus eventBus, IPauseService pauseService, WindowSkin skin)
        {
            _eventBus = eventBus;
            _pauseService = pauseService;
            _skin = skin;
            _eventBus.Subscribe<TypingPuzzleRequestedEvent>(OnRequested);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<TypingPuzzleRequestedEvent>(OnRequested);
        }

        private void OnRequested(TypingPuzzleRequestedEvent evt)
        {
            if (_open || evt.Words == null || evt.Words.Length == 0)
            {
                return;
            }

            Build();

            _open = true;
            _openedFrame = Time.frameCount;
            _onFinished = evt.OnFinished;
            _words = evt.Words;
            _word = 0;
            _letters = 0;
            _time = Mathf.Max(1f, evt.Seconds);

            _panel.Open(evt.Title, "Type the words before the lockout. Wrong keys are ignored.");
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

            if (Pressed(keyboard))
            {
                Advance();
            }
        }

        private bool Pressed(Keyboard keyboard)
        {
            var expected = char.ToLowerInvariant(_words[_word][_letters]);
            if (expected < 'a' || expected > 'z')
            {
                // Не буква латиницы — считаем набранной, иначе слово не кончится.
                return true;
            }

            return keyboard[Key.A + (expected - 'a')].wasPressedThisFrame;
        }

        private void Advance()
        {
            _letters++;
            if (_letters < _words[_word].Length)
            {
                Paint();
                return;
            }

            _word++;
            _letters = 0;

            if (_word >= _words.Length)
            {
                Close(true);
                return;
            }

            Paint();
        }

        private void Paint()
        {
            var word = _words[_word];
            _wordText.text = _letters > 0
                ? $"<color=#{_typedHex}>{word.Substring(0, _letters)}</color>{word.Substring(_letters)}"
                : word;

            _panel.SetStatus($"Word {_word + 1} / {_words.Length}   —   {Mathf.Max(0f, _time):0.0} s");
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

            _typedHex = ColorUtility.ToHtmlStringRGB(_typed);
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

            var plate = PuzzlePanel.Box("Word", _panel.Content, _wordPlateSize, Vector2.zero);
            PuzzlePanel.Plate(plate, _skin?.Field, _skin?.PixelsPerUnit ?? 1f, _frameTint,
                _plateFallback);

            var text = PuzzlePanel.Box("Text", plate, _wordPlateSize, Vector2.zero);
            _wordText = PuzzlePanel.Label(text, _font, _wordFontSize, _textColor);
        }
    }
}
