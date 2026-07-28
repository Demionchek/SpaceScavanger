using System;
using UnityEngine;

namespace Game.UI
{
    public class CanvasFollowScript : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _offsetY;

        private void LateUpdate()
        {
            if (_target != null)
                transform.position = new Vector2(_target.position.x, _target.position.y + _offsetY);
        }
    }
}