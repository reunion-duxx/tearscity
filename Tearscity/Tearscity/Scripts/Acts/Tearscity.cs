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
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Unlocks;
using Tearscity.Scripts.Encounters;

using gorgeous.Scripts;
using soulaltar.Scripts;
using fountain.Scripts;
using cellar.Scripts;
using closet.Scripts;
using bench.Scripts;
using sewer.Scripts;
using market.Scripts;
using vending.Scripts;
using cart.Scripts;
using archive.Scripts;
using spire.Scripts;
using trial.Scripts;

namespace Tearscity.Scripts.Acts;

/// <summary>
/// 泪水之城（City of Tears）阶段 —— 替换第一幕。
///
/// 当前目标：只要"能进入这个阶段"。
/// 因此遭遇/事件/先古之民全部沿用原版第一幕（Overgrowth）的内容，只替换背景、地图配色与标题。
/// 后续再逐个替换为自建内容。
///
/// 关键点：
/// - 类名决定内容 ID 与资源目录：Tearscity -> res://scenes/backgrounds/tearscity/
/// - Index = 0 表示第一幕；IsDefault = false，由 Entry 里的 ActEnterForce 规则强制替换第一幕
/// </summary>
public sealed class Tearscity : ActModel
{
    public override int Index => 0;

    public override bool IsDefault => false;

    public override bool IsUnlocked(UnlockState unlockState) => true;

    // ---- 地图配色（泪水之城：深蓝 + 青蓝）----
    public override Color MapTraveledColor => new Color("0E1A26");

    public override Color MapUntraveledColor => new Color("4C7089");

    public override Color MapBgColor => new Color("1B2C3D");

    // ---- 音频：不走原版 FMOD bank，改成自己播 assets/music 里的 ogg/wav ----
    // 返回空数组 = 原版 ResolveMusic 直接返回 null（不加载 bank、不播任何 FMOD 音乐），
    // 真正的音乐由 TearscityMusic + TearscityMusicPatch 接管，见 Scripts/Audio/TearscityMusic.cs。
    public override string[] BgMusicOptions => Array.Empty<string>();

    public override string[] MusicBankPaths => Array.Empty<string>();

    public override string AmbientSfx => string.Empty;

    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act1";

    public override string ChestSpineResourcePath => "res://animations/backgrounds/treasure_room/chest_room_act_1_skel_data.tres";

    public override string ChestSpineSkinNameNormal => "act1";

    public override string ChestSpineSkinNameStroke => "act1_stroke";

    // ---- 房间数量（与第一幕一致）----
    protected override int NumberOfWeakEncounters => 3;

    protected override int BaseNumberOfRooms => 15;

    // ---- 内容：Boss / 事件都用自建的，先古之民沿用原版 ----
    public override IEnumerable<EncounterModel> BossDiscoveryOrder => new EncounterModel[]
    {
        ModelDb.Encounter<Encounters.MasterEncounter>(),
        ModelDb.Encounter<Encounters.WatcherEncounter>(),
        ModelDb.Encounter<Encounters.CollectorEncounter>()
    };

    public override IEnumerable<AncientEventModel> AllAncients => new AncientEventModel[]
    {
        ModelDb.AncientEvent<Neow>()
    };

    public override IEnumerable<EventModel> AllEvents => new EventModel[]
    {
        ModelDb.Event<GorgeousEvent>(),
        ModelDb.Event<SoulAltarEvent>(),
        ModelDb.Event<FountainEvent>(),
        ModelDb.Event<CellarEvent>(),
        ModelDb.Event<ClosetEvent>(),
        ModelDb.Event<BenchEvent>(),
        ModelDb.Event<SewerEvent>(),
        ModelDb.Event<MarketEvent>(),
        ModelDb.Event<VendingEvent>(),
        ModelDb.Event<CartEvent>(),
        ModelDb.Event<ArchiveEvent>(),
        ModelDb.Event<SpireEvent>(),
        ModelDb.Event<TrialEvent>()
    };

    public override IEnumerable<EncounterModel> GenerateAllEncounters()
    {
        return new EncounterModel[]
        {
            ModelDb.Encounter<Encounters.HunksentryEncounter>(),
            ModelDb.Encounter<Encounters.GluttonousHuskEncounter>(),
            ModelDb.Encounter<Encounters.WingedSentryEncounter>(),
            ModelDb.Encounter<Encounters.LanceSentryEncounter>(),
            ModelDb.Encounter<Encounters.BelflySwarmEncounter>(),
            ModelDb.Encounter<Encounters.HuskDandyEncounter>(),
            ModelDb.Encounter<Encounters.HunkSentryPairEncounter>(),
            ModelDb.Encounter<Encounters.SentryDuoEncounter>(),
            ModelDb.Encounter<Encounters.HuskDuoEncounter>(),
            ModelDb.Encounter<Encounters.DandyAndFollyEncounter>(),
            ModelDb.Encounter<Encounters.MistakeGroupEncounter>(),

            ModelDb.Encounter<Encounters.FollyEncounter>(),
            ModelDb.Encounter<Encounters.MistakeEncounter>(),
            ModelDb.Encounter<Encounters.CowardlyHuskEncounter>(),
            ModelDb.Encounter<Encounters.FollyAndMistakeWeakEncounter>(),

            ModelDb.Encounter<Encounters.SoulEncounter>(),
            ModelDb.Encounter<Encounters.SoulWarriorEncounter>(),
            ModelDb.Encounter<Encounters.GreatEncounter>(),
            ModelDb.Encounter<Encounters.HeavyEncounter>(),

            ModelDb.Encounter<Encounters.MasterEncounter>(),
            ModelDb.Encounter<Encounters.WatcherEncounter>(),
            ModelDb.Encounter<Encounters.CollectorEncounter>(),
        };
    }

    public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState unlockState) => AllAncients.ToList();

    protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState)
    {
        _ = Patches.TopBarBossIconRefresh.ScheduleAsync();
    }

    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng)
    {
        int restCount = mapRng.NextGaussianInt(7, 1, 6, 7);
        int unknownCount = MapPointTypeCounts.StandardRandomUnknownCount(mapRng);
        return new MapPointTypeCounts(unknownCount, restCount);
    }
}
