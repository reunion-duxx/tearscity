using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace bench.Scripts;

[RegisterActEvent(typeof(Tearscity.Scripts.Acts.Tearscity))]
public sealed class BenchEvent : ModEventTemplate
{
    private const int RestHeal = 20;
    private const int GoldFind = 30;

    private string PageOption(string page, string option) =>
        InitialOptionKey(option).Replace(".pages.INITIAL.options.", ".pages." + page + ".options.");

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://Tearscity/images/events/bench.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new EventOption[]
    {
        new EventOption(this, SitDown, InitialOptionKey("SIT"), Array.Empty<IHoverTip>()),
        new EventOption(this, Search, InitialOptionKey("SEARCH"), Array.Empty<IHoverTip>()),
    };

    private async Task SitDown()
    {
        await CreatureCmd.Heal(Owner!.Creature, RestHeal, true);

        SetEventState(PageDescription("REST"), new EventOption[]
        {
            new EventOption(this, DeepSleep, PageOption("REST", "DEEP_SLEEP"), Array.Empty<IHoverTip>()),
            new EventOption(this, Rise, PageOption("REST", "RISE"), Array.Empty<IHoverTip>()),
        });
    }

    private async Task DeepSleep()
    {
        await CardPileCmd.AddCurseToDeck<Wound>(Owner!);
        await CreatureCmd.Heal(Owner!.Creature, Owner!.Creature.MaxHp, true);
        SetEventFinished(PageDescription("SLEPT"));
    }

    private Task Rise()
    {
        SetEventFinished(PageDescription("ROSE"));
        return Task.CompletedTask;
    }

    private async Task Search()
    {
        await PlayerCmd.GainGold(GoldFind, Owner!, false);
        SetEventFinished(PageDescription("SEARCHED"));
    }

}
