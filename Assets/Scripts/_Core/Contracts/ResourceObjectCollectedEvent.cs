namespace Game.Core
{
    // Крюк вытащил объект-ресурс. Считает ОБЪЕКТЫ, а не количество ресурса:
    // один обломок может дать случайное число единиц.
    public readonly struct ResourceObjectCollectedEvent
    {
    }
}
