namespace Game.Core
{
    // Запрос переключить режим интерьер <-> полёт. Направление определяет
    // получатель по текущему стейту (toggle): из полёта -> внутрь, из
    // интерьера -> в полёт.
    public readonly struct ModeSwitchRequestedEvent
    {
    }
}
