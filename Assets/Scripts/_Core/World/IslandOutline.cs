using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    // Периметр островка для радара: точки уже в мировых координатах — островок
    // статичен, и пересчитывать их каждый кадр не за чем. Компонент должен сидеть
    // на том же объекте, что и коллайдер: радар находит островки сканированием
    // физики, а не поиском по сцене.
    public sealed class IslandOutline : MonoBehaviour
    {
        private Vector2[] _points = Array.Empty<Vector2>();

        public IReadOnlyList<Vector2> Points => _points;

        public void Set(Vector2[] worldPoints) => _points = worldPoints ?? Array.Empty<Vector2>();
    }
}
