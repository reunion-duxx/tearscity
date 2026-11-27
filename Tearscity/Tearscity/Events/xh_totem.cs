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

namespace xhtotem.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Infectedcrossroads))]
public sealed class InfectedTotemEvent : ModEventTemplate
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/xh_totem.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Touch, InitialOptionKey("TOUCH"), Array.Empty<IHoverTip>()),
        new EventOption(this, Ignore, InitialOptionKey("IGNORE"), Array.Empty<IHoverTip>()),
    };

    private const int SuggestHpCost = 8;
    private const int OfferMaxHpLoss = 4;
    private const int IgnoreGold = 40;

    private Task Touch()
    {
        SetEventState(PageDescription("TOUCHED"), new EventOption[]
        {
            new EventOption(this, Offer, ModOptionKey("TOUCHED", "OFFER"), Array.Empty<IHoverTip>()),
            new EventOption(this, Refuse, ModOptionKey("TOUCHED", "REFUSE"), Array.Empty<IHoverTip>()),
        });

        return Task.CompletedTask;
    }

    private async Task Offer()
    {
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner!.Creature, OfferMaxHpLoss, false);
        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new RelicReward(Owner!) });
        SetEventFinished(PageDescription("OFFERED"));
    }

    private async Task Refuse()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            SuggestHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        SetEventFinished(PageDescription("REFUSED"));
    }

    private async Task Ignore()
    {
        await PlayerCmd.GainGold(IgnoreGold, Owner!, false);
        SetEventFinished(PageDescription("IGNORED"));
    }
}
