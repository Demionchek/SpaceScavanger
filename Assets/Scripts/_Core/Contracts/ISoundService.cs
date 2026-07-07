using UnityEngine;

namespace Game.Core
{
    public interface ISoundService
    {
        void PlayAtPosition(AudioClip clip, Vector3 position);
    }
}
