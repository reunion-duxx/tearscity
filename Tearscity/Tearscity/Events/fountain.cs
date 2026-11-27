using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rewards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace fountain.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Tearscity))]
public sealed class FountainEvent : ModEventTemplate
{
    private const int WishMaxHpCost = 4;
    private const int WishGold = 120;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/fountain.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, WishHeal, InitialOptionKey("WISH_HEAL"), Array.Empty<IHoverTip>()),
        new EventOption(this, WishForGold, InitialOptionKey("WISH_GOLD"), Array.Empty<IHoverTip>()),
        new EventOption(this, WishRelic, InitialOptionKey("WISH_RELIC"), Array.Empty<IHoverTip>()),
        new EventOption(this, Leave, InitialOptionKey("LEAVE"), Array.Empty<IHoverTip>()),
    };

    private async Task WishHeal()
    {
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner!.Creature, WishMaxHpCost, false);
        await CreatureCmd.Heal(Owner!.Creature, Owner!.Creature.MaxHp, true);
        SetEventFinished(PageDescription("HEALED"));
    }

    private async Task WishForGold()
    {
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner!.Creature, WishMaxHpCost, false);
        await PlayerCmd.GainGold(WishGold, Owner!, false);
        SetEventFinished(PageDescription("RICH"));
    }

    private async Task WishRelic()
    {
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner!.Creature, WishMaxHpCost, false);
        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new RelicReward(Owner!) });
        SetEventFinished(PageDescription("GIFTED"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEFT"));
        return Task.CompletedTask;
    }
}
