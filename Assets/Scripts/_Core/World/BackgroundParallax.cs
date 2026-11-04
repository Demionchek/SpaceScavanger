using UnityEngine;

namespace Game.Core
{
    // Вешается на дочерний объект камеры: локальная позиция здесь и есть сдвиг
    // по экрану. Фон отыгрывает долю пути камеры в обратную сторону — получается
    // ощущение глубины, а не наклейки на стекле.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BackgroundParallax : MonoBehaviour
    {
        [Tooltip("Какая доля пути камеры отыгрывается сдвигом фона.")]
        [Range(0f, 0.5f)] [SerializeField] private float _parallax = 0.03f;

        [Tooltip("Пусто — берётся Camera.main.")]
        [SerializeField] private Camera _camera;

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();

            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            if (_camera == null || _renderer.sprite == null)
            {
                return;
            }

            // Запас спрайта поверх кадра и есть предел сдвига: дальше полез бы край.
            var halfView = new Vector2(_camera.orthographicSize * _camera.aspect, _camera.orthographicSize);
            var limit = Vector2.Max((Vector2)_renderer.bounds.extents - halfView, Vector2.zero);
            var drift = -(Vector2)_camera.transform.position * _parallax;

            var local = transform.localPosition;
            local.x = Mathf.Clamp(drift.x, -limit.x, limit.x);
            local.y = Mathf.Clamp(drift.y, -limit.y, limit.y);
            transform.localPosition = local;
        }
    }
}
