using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VContainer;

namespace Game.UI
{
    public sealed class BoundaryDeathController : MonoBehaviour
    {
        [SerializeField] private Volume _volume;
        [SerializeField] private WarningBannerUI _warning;
        [SerializeField] private string _warningMessage = "Leaving the safe zone";
        [SerializeField] private float _maxIntensity = 1f;
        [SerializeField] private float _smoothSpeed = 2f;

        private IPlayerLocator _player;
        private ZoneBounds _bounds;
        private GameStateMachine _stateMachine;
        private EventBus _eventBus;
        private Vignette _vignette;
        private float _current;
        private bool _dead;
        private bool _warningShown;

        [Inject]
        public void Construct(IPlayerLocator player, ZoneBounds bounds, GameStateMachine stateMachine, EventBus eventBus)
        {
            _player = player;
            _bounds = bounds;
            _stateMachine = stateMachine;
            _eventBus = eventBus;
        }

        private void Awake()
        {
            if (_volume != null && _volume.profile.TryGet(out _vignette))
            {
                _vignette.intensity.overrideState = true;
            }
        }

        private void Update()
        {
            if (_dead)
            {
                return;
            }

            var target = TargetIntensity();
            _current = Mathf.MoveTowards(_current, target, _smoothSpeed * Time.deltaTime);

            if (_vignette != null)
            {
                _vignette.intensity.value = _current;
            }

            if (target >= _maxIntensity && _current >= _maxIntensity - 0.001f)
            {
                _dead = true;
                _eventBus.Publish(new GameOverEvent("You are lost in the void of space"));
            }
        }

        private float TargetIntensity()
        {
            if (_stateMachine.CurrentState is not SpaceFlightState)
            {
                SetWarning(false);
                return 0f;
            }

            var distance = Vector2.Distance(_player.Position, _bounds.Center);
            SetWarning(distance >= _bounds.SafeRadius - _bounds.WarnMargin);

            var overSafe = distance - _bounds.SafeRadius;
            if (overSafe <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(overSafe / _bounds.DeathDistance) * _maxIntensity;
        }

        private void SetWarning(bool visible)
        {
            if (_warning == null || _warningShown == visible)
            {
                return;
            }

            _warningShown = visible;
            _warning.SetPersistent(visible ? _warningMessage : null);
        }
    }
}
