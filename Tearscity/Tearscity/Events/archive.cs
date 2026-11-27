using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace archive.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Tearscity))]
public sealed class ArchiveEvent : ModEventTemplate
{
    private const int StudyCost = 30;
    private const int StudyMaxHp = 3;
    private const int CopyMaxHp = 2;
    private const int StealHpCost = 5;
    private const int StealGold = 60;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/archive.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Study, InitialOptionKey("STUDY"), Array.Empty<IHoverTip>()),
        new EventOption(this, Copy, InitialOptionKey("COPY"), Array.Empty<IHoverTip>()),
        new EventOption(this, Steal, InitialOptionKey("STEAL"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(StealHpCost),
        new EventOption(this, Leave, InitialOptionKey("LEAVE"), Array.Empty<IHoverTip>()),
    };

    private async Task Study()
    {
        if (Owner!.Gold < StudyCost)
        {
            SetEventFinished(PageDescription("LEFT"));
            return;
        }

        await PlayerCmd.LoseGold(StudyCost, Owner!, GoldLossType.Spent);
        await CreatureCmd.GainMaxHp(Owner!.Creature, StudyMaxHp);
        SetEventFinished(PageDescription("STUDIED"));
    }

    private async Task Copy()
    {
        await CardPileCmd.AddCurseToDeck<Wound>(Owner!);
        await CreatureCmd.GainMaxHp(Owner!.Creature, CopyMaxHp);
        SetEventFinished(PageDescription("COPIED"));
    }

    private async Task Steal()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            StealHpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        await PlayerCmd.GainGold(StealGold, Owner!, false);
        SetEventFinished(PageDescription("STOLE"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEFT"));
        return Task.CompletedTask;
    }
}
