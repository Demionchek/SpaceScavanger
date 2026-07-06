using Game.Gameplay.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [RequireComponent(typeof(Health))]
    public sealed class EnemyHealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;

        private Health _health;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void Update()
        {
            _fillImage.fillAmount = _health.MaxHealth > 0f ? _health.CurrentHealth / _health.MaxHealth : 0f;
        }
    }
}
