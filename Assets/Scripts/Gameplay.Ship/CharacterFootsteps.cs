using UnityEngine;

namespace Game.Gameplay.Ship
{
    // Шаги персонажа в интерьере: звук по таймеру, пока он бежит по полу.
    // На паузе deltaTime нулевой, так что шаги молчат сами собой.
    [RequireComponent(typeof(CharacterMovementController))]
    public sealed class CharacterFootsteps : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip[] _clips;

        [Tooltip("Пауза между шагами на полной скорости.")]
        [SerializeField] private float _interval = 0.35f;

        [Tooltip("Ниже этой скорости шаги не звучат — иначе они цокают при сползании.")]
        [SerializeField] private float _minSpeed = 0.3f;

        [SerializeField] private Vector2 _pitchRange = new(0.95f, 1.05f);

        private CharacterMovementController _movement;
        private float _timer;
        private int _last = -1;

        private void Awake()
        {
            _movement = GetComponent<CharacterMovementController>();
        }

        private void Update()
        {
            if (_source == null || _clips == null || _clips.Length == 0)
            {
                return;
            }

            if (!_movement.IsGrounded || Mathf.Abs(_movement.HorizontalVelocity) < _minSpeed)
            {
                _timer = 0f;
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer > 0f)
            {
                return;
            }

            _timer = Mathf.Max(0.05f, _interval);
            Step();
        }

        // Тот же клип подряд звучит как заевшая пластинка, поэтому берём соседний.
        private void Step()
        {
            var index = Random.Range(0, _clips.Length);
            if (_clips.Length > 1 && index == _last)
            {
                index = (index + 1) % _clips.Length;
            }

            _last = index;
            _source.pitch = Random.Range(_pitchRange.x, _pitchRange.y);
            _source.PlayOneShot(_clips[index]);
        }
    }
}
