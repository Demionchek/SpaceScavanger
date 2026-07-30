namespace Game.Core
{
    public interface ISaveable
    {
        string SaveId { get; }
        string Save();
        void Load(string json);
    }
}
