using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Game.Gameplay.Flight
{
    public sealed class HookController : MonoBehaviour
    {
        [SerializeField] private Transform _hookAnchor;
        [SerializeField] private HookProjectile _hookProjectilePrefab;
        [SerializeField] private GameObject _ropeTilePrefab;
        [SerializeField] private float _hookRange = 8f;
        [SerializeField] private float _hookSpeed = 20f;
        [SerializeField] private float _ropeTileLength = 0.5f;
        [SerializeField] private LayerMask _hookableMask;
        [SerializeField] private int _hookLevel;
        [SerializeField] private AudioSource _hookAudioSource;
        [SerializeField] private AudioClip _launchClip;
        [SerializeField] private AudioClip _retractClip;

        private PlayerContext _playerContext;
        private InputAction _hookAction;
        private HookProjectile _activeHook;
        private RopeTilePool _ropePool;
        private int _hookLevelBonus;

        public Vector2 HookAnchorPosition => _hookAnchor.position;

        public void SetHookLevelBonus(int bonus)
        {
            _hookLevelBonus = bonus;
        }

        [Inject]
        public void Construct(InputActionAsset inputActions, PlayerContext playerContext)
        {
            _playerContext = playerContext;

            var flightMap = inputActions.FindActionMap("Flight", throwIfNotFound: true);
            _hookAction = flightMap.FindAction("Hook", throwIfNotFound: true);
        }

        private void Awake()
        {
            _ropePool = new RopeTilePool(_ropeTilePrefab, transform);
        }

        private void Update()
        {
            if (_hookAction.WasPressedThisFrame() && _activeHook == null)
            {
                LaunchHook();
            }

            if (_activeHook != null)
            {
                _ropePool.Draw(_hookAnchor.position, _activeHook.transform.position, _ropeTileLength);
            }
        }

        private void LaunchHook()
        {
            _activeHook = Instantiate(_hookProjectilePrefab, _hookAnchor.position, transform.rotation);
            _activeHook.Launch(this, transform.right, _hookRange, _hookSpeed, _hookableMask, _hookLevel + _hookLevelBonus);

            if (_hookAudioSource != null && _launchClip != null)
            {
                _hookAudioSource.PlayOneShot(_launchClip);
            }
        }

        public void OnHookGrabbed(IHookable hookable)
        {
            hookable.OnGrabbed(new HookContext(_playerContext.ResourceService));
        }

        public void OnHookReturning()
        {
            if (_hookAudioSource != null && _retractClip != null)
            {
                _hookAudioSource.PlayOneShot(_retractClip);
            }
        }

        public void OnHookReturned(HookProjectile hook)
        {
            if (_activeHook == hook)
            {
                _activeHook = null;
            }

            Destroy(hook.gameObject);
            _ropePool.Clear();
        }
    }
}
