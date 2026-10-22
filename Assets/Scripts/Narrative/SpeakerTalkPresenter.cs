using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

namespace Game.Narrative
{
    // Включает анимацию говорения над тем, чьё имя стоит в начале Yarn-реплики.
    // Добавляется в список Dialogue Presenters у DialogueRunner.
    public sealed class SpeakerTalkPresenter : DialoguePresenterBase
    {
        private readonly List<SpeakerTalkAnimation> _animations = new();

        public override YarnTask OnDialogueStartedAsync()
        {
            // Говорящие живут в additive-сцене катсцены, поэтому собираем их
            // на каждом старте диалога — и выключенные тоже.
            _animations.Clear();
            _animations.AddRange(FindObjectsByType<SpeakerTalkAnimation>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            SetTalking(null);

            return YarnTask.CompletedTask;
        }

        // Возвращаемся сразу, не дожидаясь конца реплики: DialogueRunner ждёт все
        // презентеры, а при autoAdvance токен реплики никто не отменит. Поэтому
        // предыдущего говорящего гасим на следующей реплике и в конце диалога.
        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            SetTalking(line.CharacterName);
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            SetTalking(null);
            return YarnTask.CompletedTask;
        }

        // Одно имя может висеть над несколькими объектами — один и тот же
        // персонаж в разных частях катсцены, — поэтому включаем все совпадения.
        private void SetTalking(string speaker)
        {
            var hasSpeaker = !string.IsNullOrWhiteSpace(speaker);
            var found = false;

            foreach (var animation in _animations)
            {
                var talking = hasSpeaker && string.Equals(animation.Speaker, speaker, StringComparison.OrdinalIgnoreCase);
                found |= talking;

                // Повторное включение перезапустило бы анимацию посреди
                // нескольких реплик одного и того же говорящего.
                if (animation.gameObject.activeSelf != talking)
                {
                    animation.SetTalking(talking);
                }
            }

            if (hasSpeaker && !found && _animations.Count > 0)
            {
                Debug.LogWarning($"SpeakerTalkAnimation для '{speaker}' не найдена — проверь поле Speaker над персонажем");
            }
        }
    }
}
