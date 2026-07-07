using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    public sealed class EngineSoundController : MonoBehaviour
    {
        [SerializeField] private AudioSource _oneShotSource;
        [SerializeField] private AudioSource _loopSource;
        [SerializeField] private AudioClip _startClip;
        [SerializeField] private AudioClip _loopClip;
        [SerializeField] private AudioClip _stopClip;
        [SerializeField] private float _loopStartDelay = 1f;
        [SerializeField] private float _loopStopDelay = 0.1f;

        private IShipInputProvider _shipInput;
        private bool _engineOn;
        private bool _waitingForLoopStart;
        private float _loopStartTimer;
        private bool _waitingForLoopStop;
        private float _loopStopTimer;

        [Inject]
        public void Construct(IShipInputProvider shipInput)
        {
            _shipInput = shipInput;
        }

        private void Update()
        {
            var throttling = !Mathf.Approximately(_shipInput.Throttle, 0f);

            if (throttling && !_engineOn)
            {
                StartEngine();
            }
            else if (!throttling && _engineOn)
            {
                StopEngine();
            }

            if (_waitingForLoopStart)
            {
                if (!throttling) _waitingForLoopStart = false;

                _loopStartTimer -= Time.deltaTime;
                if (_loopStartTimer <= 0f)
                {
                    _waitingForLoopStart = false;
                    PlayLoop();
                }
            }

            if (_waitingForLoopStop)
            {
                _loopStopTimer -= Time.deltaTime;
                if (_loopStopTimer <= 0f)
                {
                    _waitingForLoopStop = false;
                    if (_loopSource != null)
                    {
                        _loopSource.Stop();
                    }
                }
            }
        }

        private void StartEngine()
        {
            _engineOn = true;
            _waitingForLoopStop = false;

            if (_oneShotSource != null && _startClip != null)
            {
                _oneShotSource.PlayOneShot(_startClip);
            }

            _waitingForLoopStart = true;
            _loopStartTimer = _loopStartDelay;
        }

        private void PlayLoop()
        {
            if (_loopSource == null || _loopClip == null)
            {
                return;
            }

            _loopSource.clip = _loopClip;
            _loopSource.loop = true;
            _loopSource.Play();
        }

        private void StopEngine()
        {
            _engineOn = false;
            _waitingForLoopStart = false;

            if (_oneShotSource != null && _stopClip != null)
            {
                _oneShotSource.PlayOneShot(_stopClip);
            }

            if (_loopSource != null && _loopSource.isPlaying)
            {
                _waitingForLoopStop = true;
                _loopStopTimer = _loopStopDelay;
            }
        }
    }
}
