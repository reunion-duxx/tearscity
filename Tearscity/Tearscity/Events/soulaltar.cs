using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace soulaltar.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Tearscity))]
public sealed class SoulAltarEvent : ModEventTemplate
{
    private const int HpCost = 8;
    private const int MaxHpGain = 4;
    private const int GoldGain = 55;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/soul_altar.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, Touch, InitialOptionKey("TOUCH"), Array.Empty<IHoverTip>())
            .ThatDoesDamage(HpCost),
        new EventOption(this, Scrape, InitialOptionKey("SCRAPE"), Array.Empty<IHoverTip>()),
    };

    private async Task Touch()
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            HpCost,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        await CreatureCmd.GainMaxHp(Owner!.Creature, MaxHpGain);
        SetEventFinished(PageDescription("TOUCHED"));
    }

    private async Task Scrape()
    {
        await PlayerCmd.GainGold(GoldGain, Owner!, false);
        SetEventFinished(PageDescription("SCRAPED"));
    }

}
