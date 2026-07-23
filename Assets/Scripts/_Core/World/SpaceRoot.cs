using UnityEngine;

namespace Game.Core
{
    // Маркер корня космоса. Под Content спавнится всё процедурное содержимое
    // (ресурсы, враги, торговцы, квест-гиверы) и очищается при регенерации зоны.
    // Сам SpaceRoot целиком гасится (SetActive) при переходе в интерьер. Игрок,
    // космическая vcam и фон — прямые дети SpaceRoot ВНЕ Content, поэтому и
    // замораживаются вместе с космосом, и переживают регенерацию.
    public sealed class SpaceRoot : MonoBehaviour
    {
        [SerializeField] private Transform _content;

        public Transform Content => _content != null ? _content : transform;
    }
}
