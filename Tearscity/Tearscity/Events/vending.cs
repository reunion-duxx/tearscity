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

namespace vending.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Tearscity))]
public sealed class VendingEvent : ModEventTemplate
{
    private const int PayCost = 40;
    private const int SoulMaxHpCost = 4;
    private const int SmashHpCost = 6;
    private const int SmashGold = 30;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/vending.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Pay, InitialOptionKey("PAY"), Array.Empty<IHoverTip>()),
        new EventOption(this, FeedSoul, InitialOptionKey("SOUL"), Array.Empty<IHoverTip>()),
        new EventOption(this, Smash, InitialOptionKey("SMASH"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(SmashHpCost),
        new EventOption(this, Leave, InitialOptionKey("LEAVE"), Array.Empty<IHoverTip>()),
    };

    private async Task Pay()
    {
        if (Owner!.Gold < PayCost)
        {
            SetEventFinished(PageDescription("LEFT"));
            return;
        }

        await PlayerCmd.LoseGold(PayCost, Owner!, GoldLossType.Spent);
        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new PotionReward(Owner!) });
        SetEventFinished(PageDescription("PAID"));
    }

    private async Task FeedSoul()
    {
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner!.Creature, SoulMaxHpCost, false);
        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new RelicReward(Owner!) });
        SetEventFinished(PageDescription("FED"));
    }

    private async Task Smash()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            SmashHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        await PlayerCmd.GainGold(SmashGold, Owner!, false);
        SetEventFinished(PageDescription("SMASHED"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEFT"));
        return Task.CompletedTask;
    }
}
