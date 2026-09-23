using UnityEngine;

namespace Game.Core
{
    // Объект-силуэт для обводки: повторяет спрайт соседнего рендерера и рисует
    // его материалом Game/2D/Sprite Outline. Материал самого спрайта не трогаем,
    // поэтому его освещение и шейдер остаются как были.
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteOutline : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _source;
        [Tooltip("Толщина в пикселях текстуры спрайта.")]
        [SerializeField, Min(0f)] private float _widthPixels = 1f;
        [SerializeField] private int _sortingOrderOffset = -1;

        private SpriteRenderer _renderer;
        private MaterialPropertyBlock _properties;
        private Sprite _syncedSprite;
        private float _syncedWidth;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _properties = new MaterialPropertyBlock();

            if (_source == null)
            {
                _source = transform.parent != null ? transform.parent.GetComponent<SpriteRenderer>() : null;
            }
        }

        private void OnEnable() => Sync();

        private void LateUpdate()
        {
            if (_source != null && (_source.sprite != _syncedSprite || !Mathf.Approximately(_widthPixels, _syncedWidth)))
            {
                Sync();
            }
        }

        private void Sync()
        {
            if (_source == null || _renderer == null)
            {
                return;
            }

            var sprite = _source.sprite;
            _syncedSprite = sprite;
            _syncedWidth = _widthPixels;

            _renderer.sprite = sprite;
            _renderer.flipX = _source.flipX;
            _renderer.flipY = _source.flipY;
            _renderer.sortingLayerID = _source.sortingLayerID;
            _renderer.sortingOrder = _source.sortingOrder + _sortingOrderOffset;

            if (sprite == null)
            {
                return;
            }

            var texture = sprite.texture;
            var rect = sprite.textureRect;
            var min = new Vector2(rect.xMin / texture.width, rect.yMin / texture.height);
            var max = new Vector2(rect.xMax / texture.width, rect.yMax / texture.height);

            _renderer.GetPropertyBlock(_properties);
            _properties.SetVector("_SpriteRect", new Vector4(min.x, min.y, max.x, max.y));
            _properties.SetVector("_OutlineOffset", new Vector4(
                _widthPixels / texture.width,
                _widthPixels / texture.height,
                _widthPixels / sprite.pixelsPerUnit,
                _widthPixels / sprite.pixelsPerUnit));
            _renderer.SetPropertyBlock(_properties);
        }
    }
}
