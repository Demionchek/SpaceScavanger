namespace Game.Core
{
    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(PlayerContext ctx);
    }
}
