using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    public sealed class PlayerTransformSaver : MonoBehaviour, ISaveable
    {
        [SerializeField] private Rigidbody2D _body;

        public string SaveId => "player_transform";

        public string Save()
        {
            var data = new SaveData
            {
                x = _body.position.x,
                y = _body.position.y,
                rotation = _body.rotation
            };

            return JsonUtility.ToJson(data);
        }

        public void Load(string json)
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                return;
            }

            _body.position = new Vector2(data.x, data.y);
            _body.rotation = data.rotation;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
        }

        [Serializable]
        private sealed class SaveData
        {
            public float x;
            public float y;
            public float rotation;
        }
    }
}
