namespace Game.Core
{
    public interface IQuotaService
    {
        // Квота активна и груз набран — игрок может сдать её сам.
        bool CanDeliver { get; }

        // Числа текущей квоты, их печатает диалог офицера. 0 — квоты нет.
        int Amount { get; }
        int SectorsLeft { get; }

        bool TryDeliver();

        // true — прыжок не состоится: срок квоты вышел, а груза нет, и корпорация
        // взрывает корабль. Набранный груз на этом прыжке списывается сам.
        bool TryEnforceDeadline();
    }
}
