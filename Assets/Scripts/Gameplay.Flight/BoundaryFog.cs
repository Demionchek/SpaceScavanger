using Game.Core;
using Game.Data;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Flight
{
    // Стена тумана по краю зоны. Вигнетка и баннер говорят «плохо», но не говорят,
    // где именно кончается карта, — туман показывает это заранее и издалека.
    // Градиент запекается в текстуру один раз, поэтому в кадре ничего не считается:
    // спрайт неподвижно висит в центре зоны.
    public sealed class BoundaryFog : IStartable
    {
        // Крупный тексель — это и есть «пиксельность»: с Point-фильтром край кольца
        // рассыпается на квадраты вместо гладкого градиента.
        private const int Resolution = 1024;

        // Запас за радиусом смерти: дальше игрок не улетит, но край спрайта не должен
        // попадать в кадр.
        private const float OuterMargin = 30f;

        // Сколько ступеней в градиенте. Меньше — грубее лесенка, как в пиксель-арте.
        private const int AlphaSteps = 7;

        private readonly ZoneBounds _bounds;
        private readonly ZoneConfig _zoneConfig;
        private readonly SpaceRoot _spaceRoot;
        private readonly Color _color;
        private readonly float _maxAlpha;
        private readonly float _hazeFraction;
        private readonly int _sortingOrder;
        private readonly float _innerRadius;
        private readonly float _outerRadius;

        public BoundaryFog(ZoneBounds bounds, ZoneConfig zoneConfig, SpaceRoot spaceRoot, Color fogColor,
            float maxAlpha, float hazeFraction, int sortingOrder, float innerRadius, float outerRadius)
        {
            _bounds = bounds;
            _zoneConfig = zoneConfig;
            _spaceRoot = spaceRoot;
            _color = fogColor;
            _maxAlpha = maxAlpha;
            _hazeFraction = Mathf.Clamp01(hazeFraction);
            _sortingOrder = sortingOrder;
            _innerRadius = innerRadius;
            _outerRadius = outerRadius;
        }

        public void Start()
        {
            if (_bounds == null || _spaceRoot == null || _maxAlpha <= 0f)
            {
                return;
            }

            var outer = _bounds.SafeRadius + _bounds.DeathDistance + OuterMargin;

            // Дымка начинается рано — стену видно издалека, — но до самого дальнего
            // объекта зоны держится лёгкой: содержимое генератор раскидывает по
            // квадрату `AreaSize`, и его угол дальше круга границы. Прятать за стеной
            // то, за чем игрок летит, нельзя, поэтому плотность набирается уже за ним.
            var fogStart = _innerRadius > 0f ? _innerRadius : _bounds.SafeRadius - _bounds.WarnMargin * 1.5f;
            var fogFull = _outerRadius > 0f ? _outerRadius : _bounds.SafeRadius + _bounds.DeathDistance;
            var contentEdge = Mathf.Clamp(ContentEdge(), fogStart, fogFull);

            var texture = BuildTexture(outer, fogStart, contentEdge, fogFull);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, Resolution, Resolution),
                new Vector2(0.5f, 0.5f), Resolution / (outer * 2f));

            // Под кораблями, но над содержимым зоны: пилот должен видеть себя и того,
            // кто за ним гонится, а всё остальное туман съедает.
            var holder = new GameObject("BoundaryFog");
            holder.transform.SetParent(_spaceRoot.transform, false);
            holder.transform.position = _bounds.Center;

            var renderer = holder.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = _sortingOrder;
        }

        // Дальний угол квадрата, по которому расставлено содержимое зоны.
        private float ContentEdge()
        {
            return _zoneConfig != null
                ? _zoneConfig.AreaSize.magnitude * 0.5f
                : _bounds.SafeRadius;
        }

        private Texture2D BuildTexture(float worldRadius, float fogStart, float contentEdge, float fogFull)
        {
            var texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false)
            {
                name = "BoundaryFog",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };

            var pixels = new Color32[Resolution * Resolution];
            var centre = (Resolution - 1) * 0.5f;

            for (var y = 0; y < Resolution; y++)
            {
                for (var x = 0; x < Resolution; x++)
                {
                    var dx = (x - centre) / centre;
                    var dy = (y - centre) / centre;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy) * worldRadius;
                    // Две ступени: до края содержимого — только дымка, дальше — стена.
                    var ramp = distance < contentEdge
                        ? Mathf.SmoothStep(0f, _hazeFraction, Mathf.InverseLerp(fogStart, contentEdge, distance))
                        : Mathf.SmoothStep(_hazeFraction, 1f, Mathf.InverseLerp(contentEdge, fogFull, distance));

                    // Ступеньки по альфе: плавный градиент выдавал бы себя как
                    // нарисованный поверх пиксельной графики.
                    var alpha = Mathf.Ceil(ramp * AlphaSteps) / AlphaSteps * _maxAlpha;

                    pixels[y * Resolution + x] = new Color(_color.r, _color.g, _color.b, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
