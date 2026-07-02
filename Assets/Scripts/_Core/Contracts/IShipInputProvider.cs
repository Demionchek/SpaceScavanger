namespace Game.Core
{
    public interface IShipInputProvider
    {
        float Throttle { get; }
        float Rotation { get; }
        bool FirePressed { get; }
        bool InteractPressed { get; }
    }
}
