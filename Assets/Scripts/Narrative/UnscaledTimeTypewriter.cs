using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

namespace Game.Narrative
{
    // Замена LetterTypewriter из Yarn Spinner: та же посимвольная печать,
    // но на unscaled time — работает при Time.timeScale = 0 (пауза мира).
    public sealed class UnscaledTimeTypewriter : MonoBehaviour, IAsyncTypewriter
    {
        [SerializeField] private int _lettersPerSecond = 60;

        public List<IActionMarkupHandler> ActionMarkupHandlers { get; } = new();
        public TMP_Text TextElement { get; set; }

        public void PrepareForContent(MarkupParseResult line)
        {
            if (TextElement != null)
            {
                TextElement.maxVisibleCharacters = 0;
                TextElement.text = line.Text;
            }

            foreach (var handler in ActionMarkupHandlers)
            {
                handler.OnPrepareForLine(line, TextElement);
            }
        }

        public async YarnTask RunTypewriter(MarkupParseResult line, CancellationToken cancellationToken)
        {
            if (TextElement == null)
            {
                Debug.LogWarning($"{nameof(UnscaledTimeTypewriter)} has no text element to type into");
                return;
            }

            TextElement.maxVisibleCharacters = 0;

            foreach (var handler in ActionMarkupHandlers)
            {
                handler.OnLineDisplayBegin(line, TextElement);
            }

            var secondsPerCharacter = _lettersPerSecond > 0 ? 1.0 / _lettersPerSecond : 0.0;
            var visibleCharacterCount = TextElement.GetTextInfo(line.Text).characterCount;
            var accumulatedDelay = secondsPerCharacter;

            for (int i = 0; i < visibleCharacterCount; i++)
            {
                while (!cancellationToken.IsCancellationRequested && accumulatedDelay < secondsPerCharacter)
                {
                    var timeBeforeYield = Time.unscaledTimeAsDouble;
                    await YarnTask.Yield();
                    accumulatedDelay += Time.unscaledTimeAsDouble - timeBeforeYield;
                }

                foreach (var handler in ActionMarkupHandlers)
                {
                    await handler
                        .OnCharacterWillAppear(i, line, cancellationToken)
                        .SuppressCancellationThrow();
                }

                TextElement.maxVisibleCharacters += 1;
                accumulatedDelay -= secondsPerCharacter;
            }

            TextElement.maxVisibleCharacters = visibleCharacterCount;

            foreach (var handler in ActionMarkupHandlers)
            {
                handler.OnLineDisplayComplete();
            }
        }

        public void ContentWillDismiss()
        {
            foreach (var handler in ActionMarkupHandlers)
            {
                handler.OnLineWillDismiss();
            }
        }

        public void ContentDidDismiss()
        {
            if (TextElement != null)
            {
                TextElement.maxVisibleCharacters = 0;
            }
        }
    }
}
