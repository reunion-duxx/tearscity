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

namespace xhnest.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Infectedcrossroads))]
public sealed class MawlekNestEvent : ModEventTemplate
{
    public override bool IsShared => true;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/xh_nest.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Burn, InitialOptionKey("BURN"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(BurnHpCost),
        new EventOption(this, Harvest, InitialOptionKey("HARVEST"), Array.Empty<IHoverTip>()),
        new EventOption(this, Strike, InitialOptionKey("STRIKE"), Array.Empty<IHoverTip>()),
    };

    private const int BurnHpCost = 10;
    private const int HarvestGold = 80;
    private const int StrikeGold = 60;

    private async Task Burn()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            BurnHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new RelicReward(Owner!) });
        SetEventFinished(PageDescription("BURNED"));
    }

    private async Task Harvest()
    {
        await PlayerCmd.GainGold(HarvestGold, Owner!, false);
        await CardPileCmd.AddCurseToDeck<MegaCrit.Sts2.Core.Models.Cards.Wound>(Owner!);
        SetEventFinished(PageDescription("HARVESTED"));
    }

    private Task Strike()
    {
        EnterCombatWithoutExitingEvent<Tearscity.Scripts.EncountersXh.HatchlingSwarmEncounter>(
            new Reward[]
            {
                new GoldReward(StrikeGold, Owner!, false),
                new PotionReward(Owner!),
            },
            false);

        return Task.CompletedTask;
    }
}
