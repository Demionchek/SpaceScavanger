using UnityEngine;

namespace Game.Core
{
    // Объект, который радар запоминает навсегда: попал в дальность один раз — метка
    // остаётся, а за пределами дальности прижимается к краю и показывает направление.
    public sealed class RadarLandmark : MonoBehaviour
    {
        [Tooltip("Иконка на радаре. Пусто — останется точка блипа, только крупнее.")]
        [SerializeField] private Sprite _icon;

        [SerializeField] private Color _tint = Color.white;

        public Sprite Icon => _icon;
        public Color Tint => _tint;
    }
}
