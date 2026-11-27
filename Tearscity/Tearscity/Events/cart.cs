using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace cart.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Tearscity))]
public sealed class CartEvent : ModEventTemplate
{
    private const int SearchGold = 45;
    private const int PryHpCost = 4;
    private const int DeeperGold = 60;

    private string PageOption(string page, string option) =>
        InitialOptionKey(option).Replace(".pages.INITIAL.options.", ".pages." + page + ".options.");

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/cart.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Search, InitialOptionKey("SEARCH"), Array.Empty<IHoverTip>()),
        new EventOption(this, Pry, InitialOptionKey("PRY"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(PryHpCost),
    };

    private async Task Search()
    {
        await PlayerCmd.GainGold(SearchGold, Owner!, false);

        SetEventState(PageDescription("DUG"), new EventOption[]
        {
            new EventOption(this, DigDeeper, PageOption("DUG", "DEEPER"), Array.Empty<IHoverTip>()),
            new EventOption(this, Stop, PageOption("DUG", "STOP"), Array.Empty<IHoverTip>()),
        });
    }

    private async Task DigDeeper()
    {
        await PlayerCmd.GainGold(DeeperGold, Owner!, false);
        await CardPileCmd.AddCurseToDeck<Wound>(Owner!);
        SetEventFinished(PageDescription("DEEP"));
    }

    private Task Stop()
    {
        SetEventFinished(PageDescription("STOPPED"));
        return Task.CompletedTask;
    }

    private async Task Pry()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            PryHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new PotionReward(Owner!) });
        SetEventFinished(PageDescription("PRIED"));
    }
}
