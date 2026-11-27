using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace spire.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Tearscity))]
public sealed class SpireEvent : ModEventTemplate
{
    private const int ClimbHpCost = 8;
    private const int ClimbGold = 70;
    private const int OfferCost = 60;
    private const int OfferHeal = 10;
    private const int PrayHeal = 15;

    private string PageOption(string page, string option) =>
        InitialOptionKey(option).Replace(".pages.INITIAL.options.", ".pages." + page + ".options.");

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/spire.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Climb, InitialOptionKey("CLIMB"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(ClimbHpCost),
        new EventOption(this, Pray, InitialOptionKey("PRAY"), Array.Empty<IHoverTip>()),
    };

    private async Task Climb()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            ClimbHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        await PlayerCmd.GainGold(ClimbGold, Owner!, false);

        SetEventState(PageDescription("TOP"), new EventOption[]
        {
            new EventOption(this, Offer, PageOption("TOP", "OFFER"), Array.Empty<IHoverTip>()),
            new EventOption(this, TakeAndLeave, PageOption("TOP", "END"), Array.Empty<IHoverTip>()),
        });
    }

    private async Task Offer()
    {
        if (Owner!.Gold >= OfferCost)
        {
            await PlayerCmd.LoseGold(OfferCost, Owner!, GoldLossType.Spent);
        }

        await CreatureCmd.Heal(Owner!.Creature, OfferHeal, true);
        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new RelicReward(Owner!) });
        SetEventFinished(PageDescription("OFFERED"));
    }

    private Task TakeAndLeave()
    {
        SetEventFinished(PageDescription("CLIMBED"));
        return Task.CompletedTask;
    }

    private async Task Pray()
    {
        await CreatureCmd.Heal(Owner!.Creature, PrayHeal, true);
        SetEventFinished(PageDescription("PRAYED"));
    }
}
