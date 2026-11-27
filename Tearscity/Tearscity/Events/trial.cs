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

namespace trial.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Tearscity))]
public sealed class TrialEvent : ModEventTemplate
{
    private const int TrialGold = 150;
    private const int YieldHpCost = 3;

    public override bool IsShared => true;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/trial.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Fight, InitialOptionKey("FIGHT"), Array.Empty<IHoverTip>()),
        new EventOption(this, Yield, InitialOptionKey("YIELD"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(YieldHpCost),
    };

    private Task Fight()
    {
        EnterCombatWithoutExitingEvent<Tearscity.Scripts.Encounters.SoulWarriorEncounter>(
            new Reward[]
            {
                new GoldReward(TrialGold, Owner!, false),
                new RelicReward(Owner!),
            },
            false);

        return Task.CompletedTask;
    }

    private async Task Yield()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            YieldHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        SetEventFinished(PageDescription("YIELDED"));
    }

}
