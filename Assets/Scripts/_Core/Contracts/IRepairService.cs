namespace Game.Core
{
    // Починка корпуса за ресурс. Тот же лом идёт и в квоту, и в корпус —
    // выбор между ними и есть вся механика.
    public interface IRepairService
    {
        ResourceType Resource { get; }

        // Сколько корпуса не хватает и сколько ресурса это стоило бы целиком.
        int MissingHull { get; }
        int CostToFull { get; }

        // Сколько игрок способен оплатить сейчас: починка идёт на то, что есть.
        int AffordableCost { get; }

        // Сколько корпуса вернёт нажатие прямо сейчас.
        int RepairableHull { get; }
        bool CanRepair { get; }

        // Возвращает восстановленные единицы корпуса, 0 — ничего не вышло.
        int Repair();
    }
}
