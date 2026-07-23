using System.Collections;
using Game.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    // Оркестрирует переход интерьер <-> полёт "обложкой" (whiteout): наезд
    // космической камеры + засветление, под белым — мгновенный swap (заморозка
    // космоса, загрузка/выгрузка additive-сцены интерьера, смена стейта), затем
    // отъезд камеры интерьера + гашение белого. Должен жить ВНЕ _spaceRoot,
    // иначе его корутина оборвётся при заморозке космоса.
    public sealed class ModeTransitionController : MonoBehaviour
    {
        [SerializeField] private Image _fadeImage;
        [SerializeField] private CinemachineCamera _spaceCamera;
        [SerializeField] private string _interiorSceneName = "ShipInterior";
        [SerializeField] private float _fadeDuration = 0.4f;
        [SerializeField] private float _zoomInSize = 1.5f;

        private EventBus _eventBus;
        private GameStateMachine _stateMachine;
        private IPauseService _pauseService;
        private InputActionAsset _inputActions;
        private SpaceRoot _spaceRoot;
        private ModeLock _modeLock;

        private float _spaceDefaultSize;
        private bool _transitioning;

        [Inject]
        public void Construct(EventBus eventBus, GameStateMachine stateMachine,
            IPauseService pauseService, InputActionAsset inputActions, SpaceRoot spaceRoot, ModeLock modeLock)
        {
            _eventBus = eventBus;
            _stateMachine = stateMachine;
            _pauseService = pauseService;
            _inputActions = inputActions;
            _spaceRoot = spaceRoot;
            _modeLock = modeLock;

            // Подписываемся здесь, а не в OnEnable: инъекция может выполниться
            // позже OnEnable. Контроллер персистентный, отписка в OnDestroy.
            _eventBus.Subscribe<ModeSwitchRequestedEvent>(OnModeSwitchRequested);
        }

        private void Awake()
        {
            _spaceDefaultSize = _spaceCamera.Lens.OrthographicSize;

            if (_fadeImage == null)
            {
                Debug.LogError("ModeTransitionController: не назначен _fadeImage — экран не будет белеть.");
            }

            SetFadeAlpha(0f);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<ModeSwitchRequestedEvent>(OnModeSwitchRequested);
        }

        private void OnModeSwitchRequested(ModeSwitchRequestedEvent _)
        {
            if (_transitioning)
            {
                return;
            }

            if (_stateMachine.CurrentState is SpaceFlightState)
            {
                // Гейт: вход в интерьер запрещён на паузе (диалог) и под ModeLock
                // (бой, гонка).
                if (_pauseService.IsPaused || _modeLock.IsLocked)
                {
                    return;
                }

                StartCoroutine(EnterInterior());
            }
            else if (_stateMachine.CurrentState is ShipInteriorState)
            {
                StartCoroutine(EnterFlight());
            }
        }

        private IEnumerator EnterInterior()
        {
            _transitioning = true;
            _inputActions.Disable();

            yield return Cover(_spaceCamera, _spaceDefaultSize, _zoomInSize);

            _spaceCamera.Lens.OrthographicSize = _spaceDefaultSize;
            _spaceRoot.gameObject.SetActive(false);
            yield return SceneManager.LoadSceneAsync(_interiorSceneName, LoadSceneMode.Additive);
            _stateMachine.ChangeState<ShipInteriorState>();

            var interiorCamera = FindSceneCamera(_interiorSceneName);
            var interiorDefaultSize = interiorCamera != null ? interiorCamera.Lens.OrthographicSize : _spaceDefaultSize;
            if (interiorCamera != null)
            {
                interiorCamera.Lens.OrthographicSize = _zoomInSize;
            }

            yield return Reveal(interiorCamera, _zoomInSize, interiorDefaultSize);
            _transitioning = false;
        }

        private IEnumerator EnterFlight()
        {
            _transitioning = true;
            _inputActions.Disable();

            var interiorCamera = FindSceneCamera(_interiorSceneName);
            var interiorDefaultSize = interiorCamera != null ? interiorCamera.Lens.OrthographicSize : _spaceDefaultSize;

            yield return Cover(interiorCamera, interiorDefaultSize, _zoomInSize);

            var interiorScene = SceneManager.GetSceneByName(_interiorSceneName);
            if (interiorScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(interiorScene);
            }

            _spaceRoot.gameObject.SetActive(true);
            _stateMachine.ChangeState<SpaceFlightState>();
            _spaceCamera.Lens.OrthographicSize = _zoomInSize;

            yield return Reveal(_spaceCamera, _zoomInSize, _spaceDefaultSize);
            _transitioning = false;
        }

        // Засветление 0->1 одновременно с наездом камеры from->to.
        private IEnumerator Cover(CinemachineCamera camera, float fromSize, float toSize)
        {
            for (var elapsed = 0f; elapsed < _fadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                var k = elapsed / _fadeDuration;
                SetFadeAlpha(k);
                if (camera != null)
                {
                    camera.Lens.OrthographicSize = Mathf.Lerp(fromSize, toSize, k);
                }

                yield return null;
            }

            SetFadeAlpha(1f);
            if (camera != null)
            {
                camera.Lens.OrthographicSize = toSize;
            }
        }

        // Гашение белого 1->0 одновременно с отъездом камеры from->to.
        private IEnumerator Reveal(CinemachineCamera camera, float fromSize, float toSize)
        {
            for (var elapsed = 0f; elapsed < _fadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                var k = elapsed / _fadeDuration;
                SetFadeAlpha(1f - k);
                if (camera != null)
                {
                    camera.Lens.OrthographicSize = Mathf.Lerp(fromSize, toSize, k);
                }

                yield return null;
            }

            SetFadeAlpha(0f);
            if (camera != null)
            {
                camera.Lens.OrthographicSize = toSize;
            }
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

        private static CinemachineCamera FindSceneCamera(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid())
            {
                return null;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                var camera = root.GetComponentInChildren<CinemachineCamera>(true);
                if (camera != null)
                {
                    return camera;
                }
            }

            return null;
        }
    }
}
