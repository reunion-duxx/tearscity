using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace market.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Tearscity))]
public sealed class MarketEvent : ModEventTemplate
{
    private const int BuyCost = 60;
    private const int SellGold = 35;
    private const int SellHpCost = 6;
    private const int StealHpCost = 10;
    private const int StealGold = 90;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/market.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Buy, InitialOptionKey("BUY"), Array.Empty<IHoverTip>()),
        new EventOption(this, Sell, InitialOptionKey("SELL"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(SellHpCost),
        new EventOption(this, Steal, InitialOptionKey("STEAL"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(StealHpCost),
        new EventOption(this, Leave, InitialOptionKey("LEAVE"), Array.Empty<IHoverTip>()),
    };

    private async Task Buy()
    {
        if (Owner!.Gold < BuyCost)
        {
            SetEventFinished(PageDescription("LEFT"));
            return;
        }

        await PlayerCmd.LoseGold(BuyCost, Owner!, GoldLossType.Spent);
        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new RelicReward(Owner!) });
        SetEventFinished(PageDescription("BOUGHT"));
    }

    private async Task Sell()
    {
        await PlayerCmd.GainGold(SellGold, Owner!, false);

        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            SellHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        SetEventFinished(PageDescription("SOLD"));
    }

    private async Task Steal()
    {
        await PlayerCmd.GainGold(StealGold, Owner!, false);

        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            StealHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        await CardPileCmd.AddCurseToDeck<Doubt>(Owner!);
        SetEventFinished(PageDescription("STOLE"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEFT"));
        return Task.CompletedTask;
    }
}
