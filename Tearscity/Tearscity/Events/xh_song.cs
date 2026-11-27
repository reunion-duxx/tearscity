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

namespace xhsong.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Infectedcrossroads))]
public sealed class MinerSongEvent : ModEventTemplate
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/xh_song.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Listen, InitialOptionKey("LISTEN"), Array.Empty<IHoverTip>()),
        new EventOption(this, Answer, InitialOptionKey("ANSWER"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(AnswerHpCost),
        new EventOption(this, Hum, InitialOptionKey("HUM"), Array.Empty<IHoverTip>()),
    };

    private const int ListenHeal = 18;
    private const int ListenHpCost = 12;
    private const int AnswerHpCost = 6;
    private const int HumGold = 30;

    private async Task Listen()
    {
        if (Rng.NextBool())
        {
            await CreatureCmd.Heal(Owner!.Creature, ListenHeal, true);
            SetEventFinished(PageDescription("SOOTHED"));
            return;
        }

        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            ListenHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        await CardPileCmd.AddCurseToDeck<MegaCrit.Sts2.Core.Models.Cards.Doubt>(Owner!);
        SetEventFinished(PageDescription("MADDENED"));
    }

    private async Task Answer()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            AnswerHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        await RewardsCmd.OfferCustom(Owner!, new List<Reward> { new PotionReward(Owner!) });
        SetEventFinished(PageDescription("ANSWERED"));
    }

    private async Task Hum()
    {
        await PlayerCmd.GainGold(HumGold, Owner!, false);
        SetEventFinished(PageDescription("HUMMED"));
    }
}
