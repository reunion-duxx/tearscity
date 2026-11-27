using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Rooms = MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TearscityAct = Tearscity.Scripts.Acts.Tearscity;
// 坑：怪物的命名空间和类同名（hunksentry.Scripts.hunksentry / folly.Scripts.folly），
// 在类型参数位置直接写 hunksentry / folly 会被解析成命名空间，所以这里起别名。
using HunkSentryMonster = hunksentry.Scripts.hunksentry;
using FollyMonster = folly.Scripts.folly;
using SoulMonster = soul.Scripts.SOUL;
using GreatMonster = great.Scripts.GREAT;
using HeavyMonster = heavy.Scripts.HEAVY;
using MasterMonster = master.Scripts.MASTER;
using WatcherMonster = watcher.Scripts.WATCHER;
using CollectorMonster = collector.Scripts.COLLECTOR;
using MistakeMonster = mistake.Scripts.MISTAKE;
using CowardlyMonster = cowardly.Scripts.COWARDLY;
using GluttonousMonster = gluttonous.Scripts.GLUTTONOUS;
using BelflyMonster = belfly.Scripts.BELFLY;
using WingedSentryMonster = winged.Scripts.WINGED;
using LanceSentryMonster = lance.Scripts.LANCE;
using SoulWarriorMonster = soulwarrior.Scripts.SOUL_WARRIOR;
using HuskDandyMonster = huskdandy.Scripts.HUSK_DANDY;
using GorgeousMonster = gorgeoushusk.Scripts.GORGEOUS;

namespace Tearscity.Scripts.Encounters;

/// <summary>
/// 只打一只怪的普通遭遇基类。
///
/// <para>
/// 遭遇"必须"是各自一个类（模型 ID 由类名推导：<c>&lt;MODID&gt;_ENCOUNTER_&lt;类名&gt;</c>），
/// 但<b>不需要各占一个文件</b> —— 把这堆子类都写在同一个文件里就行，
/// 一个新遭遇就等于一行：
/// <code>[RegisterActEncounter(typeof(TearscityAct))] public class XxxEncounter : SingleMonsterEncounter&lt;Xxx&gt; { }</code>
/// </para>
/// </summary>
public abstract class SingleMonsterEncounter<TMonster> : ModEncounterTemplate
    where TMonster : MonsterModel
{
    // 属性名和类型名都叫 RoomType，直接写 RoomType.Monster 会被解析成"属性.成员"，必须写全
    public override RoomType RoomType => Rooms.RoomType.Monster;

    // 图鉴 / 预加载用：规范实例，不要 ToMutable
    public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { ModelDb.Monster<TMonster>() };

    // 战斗里实际生成的必须是可变副本
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new (MonsterModel, string?)[] { (ModelDb.Monster<TMonster>().ToMutable(), null) };

    public override bool IsValidForAct(ActModel act) => act is TearscityAct;
}

/// <summary>
/// 只打一只怪的<b>精英</b>遭遇基类：和普通版唯一差别是 <see cref="RoomType"/>。
///
/// <para>
/// 精英怪必须是精英遭遇（<c>RoomType.Elite</c>）才会被 <c>ActModel</c> 的精英池选中，
/// 光把怪物写成 <c>[RegisterMonster]</c> 是不会进精英池的。
/// </para>
/// </summary>
public abstract class SingleMonsterEliteEncounter<TMonster> : SingleMonsterEncounter<TMonster>
    where TMonster : MonsterModel
{
    public override RoomType RoomType => Rooms.RoomType.Elite;
}

public abstract class SingleMonsterWeakEncounter<TMonster> : SingleMonsterEncounter<TMonster>
    where TMonster : MonsterModel
{
    public override bool IsWeak => true;
}

public abstract class SingleMonsterBossEncounter<TMonster> : SingleMonsterEncounter<TMonster>
    where TMonster : MonsterModel
{
    public override RoomType RoomType => Rooms.RoomType.Boss;
}

public abstract class GroupBossEncounter<TMonster> : ModEncounterTemplate
    where TMonster : MonsterModel
{
    protected virtual int Count => 3;

    public override RoomType RoomType => Rooms.RoomType.Boss;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { ModelDb.Monster<TMonster>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        Enumerable.Range(0, Count)
            .Select(_ => (ModelDb.Monster<TMonster>().ToMutable(), (string?)null))
            .ToArray();

    public override bool IsValidForAct(ActModel act) => act is TearscityAct;
}

public abstract class GroupEncounter<TMonster> : ModEncounterTemplate
    where TMonster : MonsterModel
{
    protected virtual int Count => 2;

    public override RoomType RoomType => Rooms.RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { ModelDb.Monster<TMonster>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        Enumerable.Range(0, Count)
            .Select(_ => (ModelDb.Monster<TMonster>().ToMutable(), (string?)null))
            .ToArray();

    public override bool IsValidForAct(ActModel act) => act is TearscityAct;
}

/// <summary>
/// 两只怪一起上的普通遭遇基类（站位由游戏自动安排）。
///
/// <para>
/// 想加这样一场遭遇，照抄一行即可：
/// <code>[RegisterActEncounter(typeof(TearscityAct))] public class HunkAndFollyEncounter : PairEncounter&lt;HunkSentryMonster, FollyMonster&gt; { }</code>
/// </para>
/// </summary>
public abstract class PairEncounter<TFirst, TSecond> : ModEncounterTemplate
    where TFirst : MonsterModel
    where TSecond : MonsterModel
{
    public override RoomType RoomType => Rooms.RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<TFirst>(),
        ModelDb.Monster<TSecond>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() => new (MonsterModel, string?)[]
    {
        (ModelDb.Monster<TFirst>().ToMutable(), null),
        (ModelDb.Monster<TSecond>().ToMutable(), null),
    };

    public override bool IsValidForAct(ActModel act) => act is TearscityAct;
}

public abstract class WeakPairEncounter<TFirst, TSecond> : PairEncounter<TFirst, TSecond>
    where TFirst : MonsterModel
    where TSecond : MonsterModel
{
    public override bool IsWeak => true;
}

// ---- 具体遭遇（一行一个）----

/// <summary>躯壳哨兵（hunksentry）单独出场。</summary>
[RegisterActEncounter(typeof(TearscityAct))]
public class HunksentryEncounter : SingleMonsterEncounter<HunkSentryMonster>
{
}

/// <summary>folly 单独出场（弱怪，会出现在幕开头的几场战斗）。</summary>
[RegisterActEncounter(typeof(TearscityAct))]
public class FollyEncounter : SingleMonsterWeakEncounter<FollyMonster>
{
}

/// <summary>soul 单独出场（精英）。</summary>
[RegisterActEncounter(typeof(TearscityAct))]
public class SoulEncounter : SingleMonsterEliteEncounter<SoulMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class GreatEncounter : SingleMonsterEliteEncounter<GreatMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class HeavyEncounter : SingleMonsterEliteEncounter<HeavyMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class MasterEncounter : SingleMonsterBossEncounter<MasterMonster>
{
    public override string BossNodePath => "res://Tearscity/images/map/tearscity_encounter_master_encounter_nodeicon";

    public override EncounterAssetProfile AssetProfile => new(
        MapNodeAssetPaths: new[]
        {
            "res://Tearscity/images/map/tearscity_encounter_master_encounter_nodeicon.png",
            "res://Tearscity/images/map/tearscity_encounter_master_encounter_nodeicon_outline.png",
        },
        RunHistoryIconPath: "res://Tearscity/images/map/tearscity_encounter_master_encounter_icon.png",
        RunHistoryIconOutlinePath: "res://Tearscity/images/map/tearscity_encounter_master_encounter_icon_outline.png");
}

[RegisterActEncounter(typeof(TearscityAct))]
public class WatcherEncounter : GroupBossEncounter<WatcherMonster>
{
    public override string BossNodePath => "res://Tearscity/images/map/tearscity_encounter_watcher_encounter_nodeicon";

    public override EncounterAssetProfile AssetProfile => new(
        MapNodeAssetPaths: new[]
        {
            "res://Tearscity/images/map/tearscity_encounter_watcher_encounter_nodeicon.png",
            "res://Tearscity/images/map/tearscity_encounter_watcher_encounter_nodeicon_outline.png",
        },
        RunHistoryIconPath: "res://Tearscity/images/map/tearscity_encounter_watcher_encounter_icon.png",
        RunHistoryIconOutlinePath: "res://Tearscity/images/map/tearscity_encounter_watcher_encounter_icon_outline.png");
}

[RegisterActEncounter(typeof(TearscityAct))]
public class CollectorEncounter : SingleMonsterBossEncounter<CollectorMonster>
{
    public override string BossNodePath => "res://Tearscity/images/map/tearscity_encounter_collector_encounter_nodeicon";

    public override IReadOnlyList<string> Slots => new[] { "jar1", "collector", "jar2" };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<CollectorMonster>().ToMutable(), "collector"),
        };

    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://scenes/encounters/tearscity_encounter_collector_encounter.tscn",
        ExtraAssetPaths: new[]
        {
            "res://scenes/creature_visuals/tearscity_monster_folly.tscn",
        },
        MapNodeAssetPaths: new[]
        {
            "res://Tearscity/images/map/tearscity_encounter_collector_encounter_nodeicon.png",
            "res://Tearscity/images/map/tearscity_encounter_collector_encounter_nodeicon_outline.png",
        },
        RunHistoryIconPath: "res://Tearscity/images/map/tearscity_encounter_collector_encounter_icon.png",
        RunHistoryIconOutlinePath: "res://Tearscity/images/map/tearscity_encounter_collector_encounter_icon_outline.png");
}

[RegisterActEncounter(typeof(TearscityAct))]
public class MistakeEncounter : SingleMonsterWeakEncounter<MistakeMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class CowardlyHuskEncounter : SingleMonsterWeakEncounter<CowardlyMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class GluttonousHuskEncounter : SingleMonsterEncounter<GluttonousMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class WingedSentryEncounter : SingleMonsterEncounter<WingedSentryMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class LanceSentryEncounter : SingleMonsterEncounter<LanceSentryMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class BelflySwarmEncounter : GroupEncounter<BelflyMonster>
{
    protected override int Count => 3;
}

[RegisterActEncounter(typeof(TearscityAct))]
public class SoulWarriorEncounter : SingleMonsterEliteEncounter<SoulWarriorMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class HuskDandyEncounter : SingleMonsterEncounter<HuskDandyMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class GorgeousEventEncounter : SingleMonsterEncounter<GorgeousMonster>
{
    public override bool IsValidForAct(ActModel act) => false;
}

[RegisterActEncounter(typeof(TearscityAct))]
public class HunkSentryPairEncounter : PairEncounter<HunkSentryMonster, HunkSentryMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class SentryDuoEncounter : PairEncounter<WingedSentryMonster, LanceSentryMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class HuskDuoEncounter : PairEncounter<GluttonousMonster, CowardlyMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class DandyAndFollyEncounter : PairEncounter<HuskDandyMonster, FollyMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class MistakeGroupEncounter : GroupEncounter<MistakeMonster>
{
}

[RegisterActEncounter(typeof(TearscityAct))]
public class FollyAndMistakeWeakEncounter : WeakPairEncounter<FollyMonster, MistakeMonster>
{
}
