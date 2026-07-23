namespace Game.Core
{
    // Запрос перегенерировать зону: очистить содержимое космоса (всё под
    // SpaceRoot.Content, т.е. всё кроме игрока и фиксированных объектов) и
    // сгенерировать заново.
    public readonly struct ZoneRegenerateRequestedEvent
    {
    }
}
