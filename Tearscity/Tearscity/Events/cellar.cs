using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace cellar.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Tearscity))]
public sealed class CellarEvent : ModEventTemplate
{
    private const int ChestGold = 70;
    private const int ChestHpCost = 7;
    private const int DrinkHeal = 12;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/cellar.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, PickLock, InitialOptionKey("PICK_LOCK"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(ChestHpCost),
        new EventOption(this, Drink, InitialOptionKey("DRINK"), Array.Empty<IHoverTip>()),
    };

    private async Task PickLock()
    {
        await PlayerCmd.GainGold(ChestGold, Owner!, false);

        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            ChestHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        SetEventFinished(PageDescription("PICKED"));
    }

    private async Task Drink()
    {
        await CreatureCmd.Heal(Owner!.Creature, DrinkHeal, true);
        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new PotionReward(Owner!) });
        SetEventFinished(PageDescription("DRANK"));
    }

}
