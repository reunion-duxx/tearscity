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
using Tearscity.Scripts.EncountersXh;

namespace Tearscity.Scripts.Acts;

public sealed class Infectedcrossroads : ActModel
{
    private static readonly string[] MusicOptions =
    {
        "event:/music/act3_a1_v1",
        "event:/music/act3_a2_v1"
    };

    private static readonly string[] MusicBanks =
    {
        "res://banks/desktop/act3_a1.bank",
        "res://banks/desktop/act3_a2.bank"
    };

    public override int Index => 2;

    public override bool IsDefault => false;

    public override bool IsUnlocked(UnlockState unlockState) => true;

    public override Color MapTraveledColor => new Color("2B1712");

    public override Color MapUntraveledColor => new Color("C9863F");

    public override Color MapBgColor => new Color("33201A");

    public override string[] BgMusicOptions => MusicOptions;

    public override string[] MusicBankPaths => MusicBanks;

    public override string AmbientSfx => "event:/sfx/ambience/act3_ambience";

    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act3";

    public override string ChestSpineResourcePath => "res://animations/backgrounds/treasure_room/chest_room_act_3_skel_data.tres";

    public override string ChestSpineSkinNameNormal => "act3";

    public override string ChestSpineSkinNameStroke => "act3_stroke";

    protected override int NumberOfWeakEncounters => 2;

    protected override int BaseNumberOfRooms => 13;

    public override IEnumerable<EncounterModel> BossDiscoveryOrder => new EncounterModel[]
    {
        ModelDb.Encounter<QueenBoss>(),
        ModelDb.Encounter<TestSubjectBoss>(),
        ModelDb.Encounter<AeonglassBoss>()
    };

    public override IEnumerable<AncientEventModel> AllAncients => new AncientEventModel[]
    {
        ModelDb.AncientEvent<Nonupeipe>(),
        ModelDb.AncientEvent<Tanx>(),
        ModelDb.AncientEvent<Vakuu>()
    };

    public override IEnumerable<EventModel> AllEvents => new EventModel[]
    {
        ModelDb.Event<xhspring.Scripts.InfectedSpringEvent>(),
        ModelDb.Event<xhstag.Scripts.StagStationEvent>(),
        ModelDb.Event<xhgrub.Scripts.GrubfatherEvent>(),
        ModelDb.Event<xhsong.Scripts.MinerSongEvent>(),
        ModelDb.Event<xhknight.Scripts.FalseKnightEvent>(),
        ModelDb.Event<xhnest.Scripts.MawlekNestEvent>(),
        ModelDb.Event<xhtotem.Scripts.InfectedTotemEvent>()
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
            Log.Info($"[Tearscity] crossroads act pool: total={all.Count} weak={weak} normal={all.Count - weak - elite - boss} elite={elite} boss={boss}"
                     + $" | events={AllEvents.Count()} ancients={AllAncients.Count()}"
                     + $" | bossOrder={string.Join(",", BossDiscoveryOrder.Select(e => e.Id.Entry))}");
        }
        catch (Exception e)
        {
            Log.Warn("[Tearscity] crossroads act pool log failed: " + e.Message, 2);
        }
    }

    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng)
    {
        int restCount = mapRng.NextGaussianInt(5, 1, 5, 6);
        int unknownCount = MapPointTypeCounts.StandardRandomUnknownCount(mapRng);
        return new MapPointTypeCounts(unknownCount, restCount);
    }

    public override IEnumerable<EncounterModel> GenerateAllEncounters()
    {
        return new EncounterModel[]
        {
            ModelDb.Encounter<TiktikPairEncounter>(),
            ModelDb.Encounter<InfectedBlobSwarmEncounter>(),
            ModelDb.Encounter<FuriousVengeflyDuoEncounter>(),
            ModelDb.Encounter<HatchlingSwarmEncounter>(),

            ModelDb.Encounter<ViolentHuskEncounter>(),
            ModelDb.Encounter<SlobberingHuskEncounter>(),
            ModelDb.Encounter<AspidHunterEncounter>(),
            ModelDb.Encounter<AspidMotherEncounter>(),
            ModelDb.Encounter<VolatileGruzzerEncounter>(),
            ModelDb.Encounter<FuriousVengeflyPackEncounter>(),
            ModelDb.Encounter<HusksPairEncounter>(),
            ModelDb.Encounter<TiktikBlobPairEncounter>(),

            ModelDb.Encounter<VengeflyKingEliteEncounter>(),
            ModelDb.Encounter<HuskGuardEliteEncounter>(),
            ModelDb.Encounter<ViolentHuskChargedEliteEncounter>(),

            ModelDb.Encounter<QueenBoss>(),
            ModelDb.Encounter<TestSubjectBoss>(),
            ModelDb.Encounter<AeonglassBoss>(),
        };
    }
}
