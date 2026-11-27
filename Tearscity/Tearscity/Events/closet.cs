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

namespace closet.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Tearscity))]
public sealed class ClosetEvent : ModEventTemplate
{
    private const int SmashHpCost = 6;
    private const int SmashGold = 25;
    private const int LooseGold = 40;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/closet.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Smash, InitialOptionKey("SMASH"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(SmashHpCost),
        new EventOption(this, TakeLoose, InitialOptionKey("TAKE"), Array.Empty<IHoverTip>()),
    };

    private async Task Smash()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            SmashHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        await PlayerCmd.GainGold(SmashGold, Owner!, false);
        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new RelicReward(Owner!) });
        SetEventFinished(PageDescription("SMASHED"));
    }

    private async Task TakeLoose()
    {
        await PlayerCmd.GainGold(LooseGold, Owner!, false);
        SetEventFinished(PageDescription("TOOK"));
    }

}
