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

namespace xhgrub.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Infectedcrossroads))]
public sealed class GrubfatherEvent : ModEventTemplate
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/xh_grub.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Free, InitialOptionKey("FREE"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(FreeHpCost),
        new EventOption(this, Take, InitialOptionKey("TAKE"), Array.Empty<IHoverTip>()),
        new EventOption(this, Feed, InitialOptionKey("FEED"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(FeedHpCost),
    };

    private const int FreeHpCost = 8;
    private const int TakeGold = 90;
    private const int FeedHpCost = 5;
    private const int FeedHeal = 20;

    private async Task Free()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            FreeHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new RelicReward(Owner!) });
        SetEventFinished(PageDescription("FREED"));
    }

    private async Task Take()
    {
        await PlayerCmd.GainGold(TakeGold, Owner!, false);
        await CardPileCmd.AddCurseToDeck<MegaCrit.Sts2.Core.Models.Cards.Wound>(Owner!);
        SetEventFinished(PageDescription("TOOK"));
    }

    private async Task Feed()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            FeedHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        await CreatureCmd.Heal(Owner!.Creature, FeedHeal, true);
        SetEventFinished(PageDescription("FED"));
    }
}
