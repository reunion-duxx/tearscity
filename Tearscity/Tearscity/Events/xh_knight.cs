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

namespace xhknight.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Infectedcrossroads))]
public sealed class FalseKnightEvent : ModEventTemplate
{
    public override bool IsShared => true;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/xh_knight.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Fight, InitialOptionKey("FIGHT"), Array.Empty<IHoverTip>()),
        new EventOption(this, Loot, InitialOptionKey("LOOT"), Array.Empty<IHoverTip>()),
        new EventOption(this, Pry, InitialOptionKey("PRY"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(PryHpCost),
    };

    private const int FightGold = 120;
    private const int PryHpCost = 12;

    private Task Fight()
    {
        EnterCombatWithoutExitingEvent<Tearscity.Scripts.EncountersXh.HuskGuardEliteEncounter>(
            new Reward[]
            {
                new GoldReward(FightGold, Owner!, false),
                new RelicReward(Owner!),
            },
            false);

        return Task.CompletedTask;
    }

    private async Task Loot()
    {
        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new RelicReward(Owner!) });
        await CardPileCmd.AddCurseToDeck<MegaCrit.Sts2.Core.Models.Cards.Wound>(Owner!);
        SetEventFinished(PageDescription("LOOTED"));
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
        await RewardsCmd.OfferCustom(Owner!, new List<Reward>
        {
            new PotionReward(Owner!),
            new PotionReward(Owner!),
        });
        SetEventFinished(PageDescription("PRIED"));
    }
}
