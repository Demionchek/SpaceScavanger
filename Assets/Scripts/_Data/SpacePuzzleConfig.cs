using UnityEngine;

namespace Game.Data
{
    // Одни числа на обе головоломки: и шахта, и спутник растут от номера сектора,
    // а не от собственных настроек префаба, иначе две находки разъехались бы.
    [CreateAssetMenu(menuName = "Game/Zone/Space Puzzle Config", fileName = "SpacePuzzleConfig")]
    public sealed class SpacePuzzleConfig : ScriptableObject
    {
        [Tooltip("К какому сектору сложность и урон выходят на максимум.")]
        public int DeepSector = 13;

        [Header("Провал")]
        [Tooltip("Эффект взрыва по корпусу. Пусто — только урон.")]
        public GameObject FailureEffect;

        [Tooltip("Сколько секунд живёт эффект.")]
        public float FailureEffectLifetime = 1.5f;

        [Tooltip("Слой отрисовки эффекта: выше кораблей (19-21), иначе взрыв уедет " +
                 "под корпус и под туман.")]
        public int FailureEffectOrder = 25;

        [Tooltip("Урон в первом секторе.")]
        public float DamageBase = 8f;

        [Tooltip("Прибавка к урону за каждый сектор после первого.")]
        public float DamagePerSector = 3f;

        [Header("Награда")]
        [Tooltip("Во сколько обломков оценивается находка. Тип и выход берутся " +
                 "у ресурса этого сектора, поэтому экономика не разъезжается.")]
        public Vector2Int RewardRocks = new(2, 3);

        [Tooltip("Шанс, что вместо ресурса достанется неизвестный чертёж.")]
        [Range(0f, 1f)] public float RecipeChance = 0.15f;

        [Header("Ползунок")]
        [Tooltip("Ширина зелёной зоны в долях полосы: в первом секторе и в глубоком.")]
        public Vector2 BandWidth = new(0.3f, 0.12f);

        [Tooltip("Скорость ползунка в полосах в секунду: в первом секторе и в глубоком.")]
        public Vector2 SliderSpeed = new(0.55f, 1.15f);

        [Tooltip("Сколько раз надо попасть: в первом секторе и в глубоком.")]
        public Vector2Int Rounds = new(1, 3);

        [Tooltip("Сколько секунд даётся на всю панель.")]
        public float SliderSeconds = 10f;

        [Header("Набор слов")]
        [Tooltip("Сколько слов надо напечатать: в первом секторе и в глубоком.")]
        public Vector2Int WordCount = new(2, 5);

        [Tooltip("Сколько секунд даётся на слово.")]
        public float SecondsPerWord = 3.5f;

        [Tooltip("Только ASCII: шрифт игры не знает других букв.")]
        public string[] Words;

        public float DamageFor(int sector) => DamageBase + DamagePerSector * Mathf.Max(0, sector - 1);

        public float BandWidthFor(int sector) => Mathf.Lerp(BandWidth.x, BandWidth.y, Progress(sector));

        public float SpeedFor(int sector) => Mathf.Lerp(SliderSpeed.x, SliderSpeed.y, Progress(sector));

        public int RoundsFor(int sector) =>
            Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(Rounds.x, Rounds.y, Progress(sector))));

        public int WordCountFor(int sector) =>
            Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(WordCount.x, WordCount.y, Progress(sector))));

        private float Progress(int sector) =>
            DeepSector <= 1 ? 1f : Mathf.Clamp01((sector - 1f) / (DeepSector - 1f));
    }
}
