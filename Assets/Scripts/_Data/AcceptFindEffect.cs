using Game.Core;

namespace Game.Data
{
    // Выбор в разговоре у находки. Эффект ничего не знает о том, какая находка
    // открыла диалог — она слушает событие сама, и слушает только одна.
    public sealed class AcceptFindEffect : ChoiceEffect
    {
        public override void Apply(GameContext ctx)
        {
            ctx.EventBus.Publish(new FindAcceptedEvent());
        }
    }
}
