using System;
using UnityEngine;

namespace Game.Core
{
    [Serializable]
    public sealed class RacePlaceReward
    {
        public ChoiceEffect[] Effects;
    }

    [CreateAssetMenu(menuName = "Game/Race/Race Definition", fileName = "RaceDefinition")]
    public sealed class RaceDefinition : ScriptableObject
    {
        // Один элемент списка = один AI-участник (префабы могут повторяться).
        public GameObject[] AiRacers;

        public GameObject CheckpointMarkerPrefab;
        public int CheckpointCount = 8;
        public float TrackRadius = 20f;
        [Range(0f, 0.5f)] public float RadiusJitter = 0.2f;
        public float CheckpointPassRadius = 3f;
        public float StartLineSpacing = 2.5f;
        public int CountdownSeconds = 3;

        // Index 0 = reward for 1st place. Last place gets nothing by design.
        public RacePlaceReward[] PlaceRewards;
    }
}
