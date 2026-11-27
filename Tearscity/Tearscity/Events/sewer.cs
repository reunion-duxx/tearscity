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

namespace sewer.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Tearscity))]
public sealed class SewerEvent : ModEventTemplate
{
    private const int BiteHpCost = 8;
    private const int FightGold = 80;

    public override bool IsShared => true;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/sewer.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Crawl, InitialOptionKey("CRAWL"), Array.Empty<IHoverTip>()),
        new EventOption(this, Reach, InitialOptionKey("REACH"), Array.Empty<IHoverTip>()),
    };

    private Task Crawl()
    {
        EnterCombatWithoutExitingEvent<Tearscity.Scripts.Encounters.BelflySwarmEncounter>(
            new Reward[]
            {
                new GoldReward(FightGold, Owner!, false),
                new PotionReward(Owner!),
            },
            false);

        return Task.CompletedTask;
    }

    private async Task Reach()
    {
        if (Rng.NextBool())
        {
            await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new RelicReward(Owner!) });
            SetEventFinished(PageDescription("FOUND"));
            return;
        }

        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            BiteHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        SetEventFinished(PageDescription("BITTEN"));
    }

}
