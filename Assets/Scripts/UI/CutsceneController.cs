using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    // Катсцена в additive-сцене: по завершении заданной Yarn-ноды гасит экран,
    // грузит сцену, играет её PlayableDirector и возвращает всё обратно.
    // Должен лежать ВНЕ _spaceRoot, иначе корутина оборвётся при его отключении.
    public sealed class CutsceneController : MonoBehaviour
    {
        [Serializable]
        public sealed class CutsceneBinding
        {
            public string AfterDialogueNode;
            public string SceneName;
            public bool AdvanceStoryOnFinish = true;
        }

        [SerializeField] private Image _fadeImage;
        [SerializeField] private float _fadeDuration = 0.4f;
        [SerializeField] private CutsceneBinding[] _cutscenes;
        [Tooltip("Снапшот MainMixer, где музыка космоса и эффекты заглушены. Назад в Gameplay вернёт PauseService.ReleasePause.")]
        [SerializeField] private AudioMixerSnapshot _cutsceneSnapshot;

        private EventBus _eventBus;
        private IPauseService _pauseService;
        private SpaceRoot _spaceRoot;

        private PlayableDirector _director;
        private string _pendingDialogue;
        private bool _directorFinished;
        private bool _running;

        [Inject]
        public void Construct(EventBus eventBus, IPauseService pauseService, SpaceRoot spaceRoot)
        {
            _eventBus = eventBus;
            _pauseService = pauseService;
            _spaceRoot = spaceRoot;

            _eventBus.Subscribe<DialogueFinishedEvent>(OnDialogueFinished);
        }

        private void Awake()
        {
            SetFadeAlpha(0f);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<DialogueFinishedEvent>(OnDialogueFinished);
        }

        public void PlayDialogue(string node)
        {
            if (!_running || string.IsNullOrEmpty(node))
            {
                return;
            }

            _pendingDialogue = node;
            SetDirectorSpeed(0d);
            _eventBus.Publish(new DialogueRequestedEvent(node));
        }

        // Сигнал в конце таймлайна: закрыть катсцену, не дожидаясь stopped
        // (при Wrap Mode = Hold его не будет вовсе).
        public void Finish()
        {
            if (_running)
            {
                _directorFinished = true;
            }
        }

        // Скорость 0, а не Pause(): граф продолжает вычисляться, поэтому сцена
        // остаётся живой, а Animator'ы крутят свои клипы, пока играет реплика.
        private void SetDirectorSpeed(double speed)
        {
            if (_director == null)
            {
                return;
            }

            var graph = _director.playableGraph;
            if (graph.IsValid())
            {
                graph.GetRootPlayable(0).SetSpeed(speed);
            }
        }

        private void OnDialogueFinished(DialogueFinishedEvent evt)
        {
            if (_pendingDialogue == evt.Node)
            {
                _pendingDialogue = null;
                SetDirectorSpeed(1d);
                return;
            }

            if (_running)
            {
                return;
            }

            var binding = FindBinding(evt.Node);
            if (binding != null)
            {
                StartCoroutine(PlayCutscene(binding));
            }
        }

        private CutsceneBinding FindBinding(string node)
        {
            if (_cutscenes == null || string.IsNullOrEmpty(node))
            {
                return null;
            }

            foreach (var binding in _cutscenes)
            {
                if (binding != null && binding.AfterDialogueNode == node && !string.IsNullOrEmpty(binding.SceneName))
                {
                    return binding;
                }
            }

            return null;
        }

        private IEnumerator PlayCutscene(CutsceneBinding binding)
        {
            _running = true;
            _pauseService.RequestPause(muteAudio: false);
            _eventBus.Publish(new CutsceneStartedEvent(binding.SceneName));

            if (_cutsceneSnapshot != null)
            {
                _cutsceneSnapshot.audioMixer.updateMode = AudioMixerUpdateMode.UnscaledTime;
                _cutsceneSnapshot.TransitionTo(_fadeDuration);
            }

            yield return Fade(0f, 1f);

            var spaceWasActive = _spaceRoot != null && _spaceRoot.gameObject.activeSelf;
            if (spaceWasActive)
            {
                _spaceRoot.gameObject.SetActive(false);
            }

            var hiddenRoots = HideAdditiveScenes();

            yield return SceneManager.LoadSceneAsync(binding.SceneName, LoadSceneMode.Additive);

            foreach (var emitter in FindAll<CutsceneDialogueEmitter>(binding.SceneName))
            {
                emitter.Bind(this);
            }

            // Игра на паузе, поэтому и таймлайн, и Animator'ы переводим на
            // немасштабированное время — иначе при timeScale = 0 всё замрёт.
            foreach (var animator in FindAll<Animator>(binding.SceneName))
            {
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }

            _director = FindDirector(binding.SceneName);
            if (_director != null)
            {
                _director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
                _directorFinished = false;
                _director.stopped += OnDirectorStopped;
                _director.time = 0d;
                _director.Play();
            }
            else
            {
                Debug.LogWarning($"Cutscene '{binding.SceneName}': PlayableDirector не найден — сцена закроется сразу");
                _directorFinished = true;
            }

            yield return Fade(1f, 0f);

            while (!_directorFinished)
            {
                if (_pendingDialogue == null && _director.time >= _director.duration)
                {
                    break;
                }

                yield return null;
            }

            if (_director != null)
            {
                _director.stopped -= OnDirectorStopped;
                _director = null;
            }

            _pendingDialogue = null;

            // Музыка катсцены уходит вместе с картинкой, а не обрывается выгрузкой сцены.
            var sources = FindAll<AudioSource>(binding.SceneName);
            var volumes = sources.ConvertAll(source => source.volume);
            yield return Fade(0f, 1f, t =>
            {
                for (var i = 0; i < sources.Count; i++)
                {
                    sources[i].volume = volumes[i] * (1f - t);
                }
            });

            var scene = SceneManager.GetSceneByName(binding.SceneName);
            if (scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }

            if (spaceWasActive)
            {
                _spaceRoot.gameObject.SetActive(true);
            }

            foreach (var root in hiddenRoots)
            {
                if (root != null)
                {
                    root.SetActive(true);
                }
            }

            yield return Fade(1f, 0f);

            _pauseService.ReleasePause();
            _running = false;

            _eventBus.Publish(new CutsceneFinishedEvent(binding.SceneName));

            if (binding.AdvanceStoryOnFinish)
            {
                _eventBus.Publish(new StoryDialogueRequestedEvent(StoryTrigger.CutsceneEnd));
            }
        }

        private void OnDirectorStopped(PlayableDirector _) => _directorFinished = true;

        // Катсцена может начаться внутри корабля: additive-сцену ShipInterior
        // (и любую другую, кроме главной) прячем так же, как SpaceRoot.
        private List<GameObject> HideAdditiveScenes()
        {
            var hidden = new List<GameObject>();
            var mainScene = gameObject.scene;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || scene == mainScene)
                {
                    continue;
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.activeSelf)
                    {
                        root.SetActive(false);
                        hidden.Add(root);
                    }
                }
            }

            return hidden;
        }

        private static PlayableDirector FindDirector(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid())
            {
                return null;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                var director = root.GetComponentInChildren<PlayableDirector>(true);
                if (director != null)
                {
                    return director;
                }
            }

            return null;
        }

        private static List<T> FindAll<T>(string sceneName) where T : Component
        {
            var found = new List<T>();
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid())
            {
                return found;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                found.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return found;
        }

        private IEnumerator Fade(float from, float to, Action<float> onProgress = null)
        {
            for (var elapsed = 0f; elapsed < _fadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                var t = elapsed / _fadeDuration;
                SetFadeAlpha(Mathf.Lerp(from, to, t));
                onProgress?.Invoke(t);
                yield return null;
            }

            SetFadeAlpha(to);
            onProgress?.Invoke(1f);
        }

        private void SetFadeAlpha(float alpha)
        {
            if (_fadeImage == null)
            {
                return;
            }

            var color = _fadeImage.color;
            color.a = alpha;
            _fadeImage.color = color;
            _fadeImage.raycastTarget = alpha > 0.01f;
        }
    }
}
