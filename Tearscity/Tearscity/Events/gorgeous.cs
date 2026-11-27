using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace gorgeous.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Tearscity))]
public sealed class GorgeousEvent : ModEventTemplate
{
    private const int HpCost = 5;
    private const int GoldAmount = 150;

    public override bool IsShared => true;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/gorgeous.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new HpLossVar(HpCost),
        new GoldVar(GoldAmount),
    };

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Leave, InitialOptionKey("LEAVE"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(DynamicVars.HpLoss.BaseValue),
        new EventOption(this, Fight, InitialOptionKey("FIGHT"), Array.Empty<IHoverTip>()),
    };

    private async Task Leave()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        SetEventFinished(PageDescription("LEFT"));
    }

    private Task Fight()
    {
        EnterCombatWithoutExitingEvent<Tearscity.Scripts.Encounters.GorgeousEventEncounter>(
            new Reward[] { new GoldReward(GoldAmount, Owner!, false) },
            false);

        return Task.CompletedTask;
    }
}
