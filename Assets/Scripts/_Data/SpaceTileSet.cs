using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Data
{
    // Набор тайлов одного вида породы. Тайл выбирается по маске соседей: бит стоит,
    // если с этой стороны островок продолжается. Имена слотов — про картинку, а не
    // про маску: угол "слева сверху" продолжается вправо и вниз.
    [CreateAssetMenu(menuName = "Game/Zone/Space Tile Set", fileName = "SpaceTileSet")]
    public sealed class SpaceTileSet : ScriptableObject
    {
        public const int North = 1;
        public const int East = 2;
        public const int South = 4;
        public const int West = 8;

        [Header("Углы")]
        public TileBase CornerTopLeft;
        public TileBase CornerTopRight;
        public TileBase CornerBottomLeft;
        public TileBase CornerBottomRight;

        [Header("Края")]
        public TileBase EdgeTop;
        public TileBase EdgeRight;
        public TileBase EdgeBottom;
        public TileBase EdgeLeft;

        [Header("Внутренние углы (вырез по диагонали)")]
        [Tooltip("Нет диагонального соседа сверху слева.")]
        public TileBase InnerTopLeft;
        [Tooltip("Нет диагонального соседа сверху справа.")]
        public TileBase InnerTopRight;
        [Tooltip("Нет диагонального соседа снизу слева.")]
        public TileBase InnerBottomLeft;
        [Tooltip("Нет диагонального соседа снизу справа.")]
        public TileBase InnerBottomRight;

        [Tooltip("Серединка. Берётся случайный из списка.")]
        public TileBase[] Fill;

        [Tooltip("Накладываются поверх серединки отдельным слоем, без коллайдера.")]
        public TileBase[] Details;

        public TileBase Resolve(int mask) => mask switch
        {
            East | South => CornerTopLeft,
            South | West => CornerTopRight,
            North | East => CornerBottomLeft,
            North | West => CornerBottomRight,
            East | South | West => EdgeTop,
            North | South | West => EdgeRight,
            North | East | West => EdgeBottom,
            North | East | South => EdgeLeft,
            // Одиночные выступы и перемычки своих картинок не имеют: узкие места
            // срезает сам генератор формы, сюда они дойти не должны.
            _ => null
        };

        // У клетки все четверо соседей, но не хватает диагонали — это вогнутый угол.
        public TileBase ResolveInner(bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
        {
            var missing = (topLeft ? 0 : 1) + (topRight ? 0 : 1) + (bottomLeft ? 0 : 1) + (bottomRight ? 0 : 1);
            if (missing != 1)
            {
                return null;
            }

            if (!topLeft) return InnerTopLeft;
            if (!topRight) return InnerTopRight;
            if (!bottomLeft) return InnerBottomLeft;
            return InnerBottomRight;
        }
    }
}
