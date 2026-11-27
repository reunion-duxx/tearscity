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

namespace xhstag.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Infectedcrossroads))]
public sealed class StagStationEvent : ModEventTemplate
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/xh_stag.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Ring, InitialOptionKey("RING"), Array.Empty<IHoverTip>()),
        new EventOption(this, Search, InitialOptionKey("SEARCH"), Array.Empty<IHoverTip>()),
        new EventOption(this, Rest, InitialOptionKey("REST"), Array.Empty<IHoverTip>()),
    };

    private const int RingGold = 80;
    private const int SearchGold = 70;
    private const int RestHeal = 15;

    private async Task Ring()
    {
        await PlayerCmd.LoseGold(RingGold, Owner!, GoldLossType.Spent);
        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new RelicReward(Owner!) });
        SetEventFinished(PageDescription("RANG"));
    }

    private async Task Search()
    {
        await PlayerCmd.GainGold(SearchGold, Owner!, false);
        await CardPileCmd.AddCurseToDeck<MegaCrit.Sts2.Core.Models.Cards.Wound>(Owner!);
        SetEventFinished(PageDescription("SEARCHED"));
    }

    private async Task Rest()
    {
        await CreatureCmd.Heal(Owner!.Creature, RestHeal, true);
        SetEventFinished(PageDescription("RESTED"));
    }
}
