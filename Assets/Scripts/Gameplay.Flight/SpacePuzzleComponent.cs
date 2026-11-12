using System.Collections.Generic;
using Game.Core;
using Game.Data;
using Game.Gameplay.Shared;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace Game.Gameplay.Flight
{
    public enum SpacePuzzleKind
    {
        Timing,
        Typing
    }

    // Находка у края островка: сначала короткий разговор о том, что перед игроком,
    // потом мини-игра. Удача даёт ресурс сектора, промах — взрыв по корпусу.
    // В обоих случаях находка уходит: иначе ставку можно было бы переигрывать,
    // пока не выпадет нужное.
    public sealed class SpacePuzzleComponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private SpacePuzzleKind _kind;
        [SerializeField] private string _prompt = "Interact";

        [Tooltip("Заголовок окна мини-игры.")]
        [SerializeField] private string _title = "FIND";

        [Tooltip("Узел Yarn с тем самым разговором: что это и чем грозит. Выбор в нём " +
                 "и запускает мини-игру через apply_effect AcceptFind.")]
        [SerializeField] private string _dialogueNode = string.Empty;

        [SerializeField] private string _successMessage = "The haul comes loose";
        [SerializeField] private string _failureMessage = "It blew up in your face";
        [SerializeField] private SpacePuzzleConfig _config;

        private EventBus _eventBus;
        private IResourceService _resources;
        private IRecipeService _recipes;
        private ISectorService _sectors;
        private SaveAssetRegistry _registry;
        private ZoneConfig _zone;
        private PlayerMarker _player;
        private Health _hull;
        private bool _started;
        private bool _waiting;
        private bool _accepted;
        private bool _dialogueOpened;

        [Inject]
        public void Construct(EventBus eventBus, IResourceService resources, IRecipeService recipes,
            ISectorService sectors, SaveAssetRegistry registry, ZoneConfig zone, PlayerMarker player,
            Health hull)
        {
            _eventBus = eventBus;
            _resources = resources;
            _recipes = recipes;
            _sectors = sectors;
            _registry = registry;
            _zone = zone;
            _player = player;
            _hull = hull;
        }

        public string Prompt => _prompt;

        private int Sector => _sectors != null ? _sectors.Current : 1;

        public void Interact(PlayerContext ctx)
        {
            if (_started || _waiting || _config == null)
            {
                return;
            }

            // Разговор ведёт Yarn, а согласие возвращается эффектом AcceptFind.
            // Мини-игра ждёт конца диалога: иначе её окно встало бы поверх реплики,
            // да ещё и второй паузой.
            if (string.IsNullOrEmpty(_dialogueNode) || _eventBus == null)
            {
                Begin();
                return;
            }

            _waiting = true;
            _accepted = false;
            _dialogueOpened = false;
            _eventBus.Subscribe<DialogueStartedEvent>(OnDialogueStarted);
            _eventBus.Subscribe<FindAcceptedEvent>(OnAccepted);
            _eventBus.Subscribe<DialogueFinishedEvent>(OnDialogueFinished);
            _eventBus.Publish(new DialogueRequestedEvent(_dialogueNode));

            // Узел не нашёлся — а один сломанный Yarn-файл роняет разом все узлы
            // в проекте. Находка от этого не должна умирать молча: играем без
            // вступления. Диалог начинается тут же, синхронно, так что к этой
            // строке уже известно, вышло или нет.
            if (!_dialogueOpened)
            {
                StopWaiting();
                Begin();
            }
        }

        private void OnDialogueStarted(DialogueStartedEvent _) => _dialogueOpened = true;

        private void OnAccepted(FindAcceptedEvent _) => _accepted = true;

        private void OnDialogueFinished(DialogueFinishedEvent _)
        {
            StopWaiting();

            if (_accepted)
            {
                Begin();
            }
        }

        // Подписка живёт только на время разговора: иначе каждая находка в секторе
        // слушала бы чужие диалоги и бросалась открывать свою мини-игру.
        private void StopWaiting()
        {
            if (!_waiting)
            {
                return;
            }

            _waiting = false;
            _eventBus.Unsubscribe<DialogueStartedEvent>(OnDialogueStarted);
            _eventBus.Unsubscribe<FindAcceptedEvent>(OnAccepted);
            _eventBus.Unsubscribe<DialogueFinishedEvent>(OnDialogueFinished);
        }

        private void OnDestroy() => StopWaiting();

        // Разговор снимает свою паузу прежде, чем дело доходит сюда: окно мини-игры
        // ставит её заново само.
        private void Begin()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            var sector = Sector;

            if (_kind == SpacePuzzleKind.Typing)
            {
                var words = Words(sector);
                _eventBus.Publish(new TypingPuzzleRequestedEvent(_title, words,
                    words.Length * _config.SecondsPerWord, Finish));
                return;
            }

            _eventBus.Publish(new TimingPuzzleRequestedEvent(_title, _config.RoundsFor(sector),
                _config.BandWidthFor(sector), _config.SpeedFor(sector), _config.SliderSeconds, Finish));
        }

        private void Finish(bool success)
        {
            if (success)
            {
                Reward();
            }
            else
            {
                Punish();
            }

            Destroy(gameObject);
        }

        private void Reward()
        {
            if (TryGrantRecipe())
            {
                return;
            }

            var sample = SectorResources.Sample(_zone, Sector);
            if (sample == null || _resources == null)
            {
                Announce(_successMessage);
                return;
            }

            var rocks = Mathf.Max(1, Random.Range(_config.RewardRocks.x, _config.RewardRocks.y + 1));
            var amount = 0;
            for (var i = 0; i < rocks; i++)
            {
                amount += sample.RollYield();
            }

            _resources.Add(sample.ResourceType, amount);
            Announce($"{_successMessage}: +{amount} {sample.ResourceType}");
        }

        // Чертёж — редкая половина награды: один стоит дороже любой руды. Знает
        // игрок уже всё — достаётся ресурс, пустой награды быть не должно.
        private bool TryGrantRecipe()
        {
            if (_recipes == null || _registry?.Recipes == null || Random.value > _config.RecipeChance)
            {
                return false;
            }

            var unknown = new List<CraftingRecipe>();
            foreach (var recipe in _registry.Recipes)
            {
                if (recipe != null && !_recipes.Knows(recipe))
                {
                    unknown.Add(recipe);
                }
            }

            if (unknown.Count == 0)
            {
                return false;
            }

            var pick = unknown[Random.Range(0, unknown.Count)];
            if (!_recipes.Learn(pick))
            {
                return false;
            }

            var title = pick.Output != null ? pick.Output.DisplayName : pick.name;
            Announce($"Schematic recovered: {title}");
            _eventBus.Publish(new NotificationRequestedEvent($"Schematic: {title}"));
            return true;
        }

        private void Punish()
        {
            var damage = _config.DamageFor(Sector);
            Blast();

            if (_hull != null)
            {
                _hull.TakeDamage(new DamageInfo
                {
                    Amount = damage,
                    Type = DamageType.Kinetic,
                    Source = gameObject
                });
            }

            Announce($"{_failureMessage}: -{Mathf.RoundToInt(damage)} hull");
        }

        // Взрыв висит на корпусе, а не в точке находки: игрок смотрит на свой
        // корабль. Порядок отрисовки задаётся здесь — в префабе эффекта он нулевой,
        // то есть под корпусом и под туманом.
        private void Blast()
        {
            if (_config.FailureEffect == null || _player == null)
            {
                return;
            }

            var effect = Instantiate(_config.FailureEffect, _player.transform.position,
                Quaternion.identity, _player.transform);
            effect.transform.localPosition = Vector3.zero;

            foreach (var sprite in effect.GetComponentsInChildren<SpriteRenderer>())
            {
                sprite.sortingOrder = _config.FailureEffectOrder;
            }

            Destroy(effect, Mathf.Max(0.1f, _config.FailureEffectLifetime));
        }

        private string[] Words(int sector)
        {
            var pool = _config.Words;
            if (pool == null || pool.Length == 0)
            {
                return new[] { "airlock" };
            }

            var indices = new List<int>(pool.Length);
            for (var i = 0; i < pool.Length; i++)
            {
                indices.Add(i);
            }

            var count = Mathf.Min(_config.WordCountFor(sector), indices.Count);
            var words = new string[count];
            for (var i = 0; i < count; i++)
            {
                var roll = Random.Range(0, indices.Count);
                words[i] = pool[indices[roll]];
                indices.RemoveAt(roll);
            }

            return words;
        }

        // Итог идёт баннером, а не в ленту: в ленте он встал бы в очередь за
        // строкой «+24 Scrap», которую та же награда и породила.
        private void Announce(string message)
        {
            _eventBus?.Publish(new WarningMessageEvent(message));
        }
    }
}
