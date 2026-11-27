using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;
using Tearscity.Scripts.EncountersFungal;

namespace Tearscity.Scripts.Acts;

public sealed class Fungalwastes : ActModel
{
    private static readonly string[] MusicOptions =
    {
        "event:/music/act2_a1_v2",
        "event:/music/act2_a2_v2"
    };

    private static readonly string[] MusicBanks =
    {
        "res://banks/desktop/act2_a1.bank",
        "res://banks/desktop/act2_a2.bank"
    };

    public override int Index => 1;

    public override bool IsDefault => false;

    public override bool IsUnlocked(UnlockState unlockState) => true;

    public override Color MapTraveledColor => new Color("2A1B36");

    public override Color MapUntraveledColor => new Color("9A85B8");

    public override Color MapBgColor => new Color("1A1420");

    public override string[] BgMusicOptions => MusicOptions;

    public override string[] MusicBankPaths => MusicBanks;

    public override string AmbientSfx => "event:/sfx/ambience/act2_ambience";

    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act2";

    public override string ChestSpineResourcePath => "res://animations/backgrounds/treasure_room/chest_room_act_2_skel_data.tres";

    public override string ChestSpineSkinNameNormal => "act2";

    public override string ChestSpineSkinNameStroke => "act2_stroke";

    protected override int NumberOfWeakEncounters => 2;

    protected override int BaseNumberOfRooms => 14;

    public override IEnumerable<EncounterModel> BossDiscoveryOrder => new EncounterModel[]
    {
        ModelDb.Encounter<MantisLordsBossEncounter>(),
        ModelDb.Encounter<ElderHuBossEncounter>()
    };

    public override IEnumerable<AncientEventModel> AllAncients => new AncientEventModel[]
    {
        ModelDb.AncientEvent<Orobas>(),
        ModelDb.AncientEvent<Pael>(),
        ModelDb.AncientEvent<Tezcatara>()
    };

    public override IEnumerable<EventModel> AllEvents => new EventModel[]
    {
        ModelDb.Event<Amalgamator>(),
        ModelDb.Event<Bugslayer>(),
        ModelDb.Event<ColorfulPhilosophers>(),
        ModelDb.Event<ColossalFlower>(),
        ModelDb.Event<FieldOfManSizedHoles>(),
        ModelDb.Event<InfestedAutomaton>(),
        ModelDb.Event<LostWisp>(),
        ModelDb.Event<SpiritGrafter>(),
        ModelDb.Event<TheLanternKey>(),
        ModelDb.Event<ZenWeaver>()
    };

    public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState unlockState) => AllAncients.ToList();

    protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState)
    {
        _ = Patches.TopBarBossIconRefresh.ScheduleAsync();
        LogActPools();
    }

    private void LogActPools()
    {
        try
        {
            var all = AllEncounters.ToList();
            var weak = all.Count(e => e.IsWeak);
            var elite = all.Count(e => e.RoomType == MegaCrit.Sts2.Core.Rooms.RoomType.Elite);
            var boss = all.Count(e => e.RoomType == MegaCrit.Sts2.Core.Rooms.RoomType.Boss);
            Log.Info($"[Tearscity] fungal act pool: total={all.Count} weak={weak} normal={all.Count - weak - elite - boss} elite={elite} boss={boss}"
                     + $" | events={AllEvents.Count()} ancients={AllAncients.Count()}"
                     + $" | bossOrder={string.Join(",", BossDiscoveryOrder.Select(e => e.Id.Entry))}");
        }
        catch (Exception e)
        {
            Log.Warn("[Tearscity] fungal act pool log failed: " + e.Message, 2);
        }
    }

    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng)
    {
        int restCount = mapRng.NextGaussianInt(6, 1, 6, 7);
        int unknownCount = MapPointTypeCounts.StandardRandomUnknownCount(mapRng);
        return new MapPointTypeCounts(unknownCount, restCount);
    }

    public override IEnumerable<EncounterModel> GenerateAllEncounters()
    {
        return new EncounterModel[]
        {
            ModelDb.Encounter<FunglingPairEncounter>(),
            ModelDb.Encounter<ShrumelingSwarmEncounter>(),
            ModelDb.Encounter<AmbloomEncounter>(),
            ModelDb.Encounter<FungifiedHuskEncounter>(),

            ModelDb.Encounter<FungoonEncounter>(),
            ModelDb.Encounter<SporgEncounter>(),
            ModelDb.Encounter<ShrumalWarriorEncounter>(),
            ModelDb.Encounter<MantisYouthEncounter>(),
            ModelDb.Encounter<FunglingPackEncounter>(),
            ModelDb.Encounter<ShrumalDuoEncounter>(),
            ModelDb.Encounter<MantisYouthDuoEncounter>(),
            ModelDb.Encounter<AmbloomFunglingEncounter>(),
            ModelDb.Encounter<SporgDuoEncounter>(),
            ModelDb.Encounter<FungoonPairEncounter>(),

            ModelDb.Encounter<ShrumalOgreEliteEncounter>(),
            ModelDb.Encounter<MantisWarriorEliteEncounter>(),
            ModelDb.Encounter<MantisPatrolEliteEncounter>(),

            ModelDb.Encounter<MantisLordsBossEncounter>(),
            ModelDb.Encounter<ElderHuBossEncounter>(),
        };
    }
}
