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

namespace xhspring.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Infectedcrossroads))]
public sealed class InfectedSpringEvent : ModEventTemplate
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/xh_spring.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Drink, InitialOptionKey("DRINK"), Array.Empty<IHoverTip>()),
        new EventOption(this, Bottle, InitialOptionKey("BOTTLE"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(BottleHpCost),
        new EventOption(this, Wash, InitialOptionKey("WASH"), Array.Empty<IHoverTip>()),
    };

    private const int DrinkHeal = 20;
    private const int DrinkMaxHpLoss = 0;
    private const int BottleHpCost = 4;
    private const int WashGold = 60;
    private const int WashHeal = 12;

    private async Task Drink()
    {
        await CreatureCmd.Heal(Owner!.Creature, DrinkHeal, true);
        await CardPileCmd.AddCurseToDeck<MegaCrit.Sts2.Core.Models.Cards.Wound>(Owner!);
        SetEventFinished(PageDescription("DRANK"));
    }

    private async Task Bottle()
    {
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner!.Creature, BottleHpCost, false);
        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new PotionReward(Owner!) });
        SetEventFinished(PageDescription("BOTTLED"));
    }

    private async Task Wash()
    {
        await PlayerCmd.LoseGold(WashGold, Owner!, GoldLossType.Spent);
        await CreatureCmd.Heal(Owner!.Creature, WashHeal, true);
        SetEventFinished(PageDescription("WASHED"));
    }
}
