using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Rooms = MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using FungalAct = Tearscity.Scripts.Acts.Fungalwastes;
using FunglingMonster = fungling.Scripts.FUNGLING;
using ShrumelingMonster = shrumeling.Scripts.SHRUMELING;
using AmbloomMonster = ambloom.Scripts.AMBLOOM;
using FungifiedHuskMonster = fungifiedhusk.Scripts.FUNGIFIED_HUSK;
using FungoonMonster = fungoon.Scripts.FUNGOON;
using SporgMonster = sporg.Scripts.SPORG;
using ShrumalWarriorMonster = shrumalwarrior.Scripts.SHRUMAL_WARRIOR;
using MantisYouthMonster = mantisyouth.Scripts.MANTIS_YOUTH;
using ShrumalOgreMonster = shrumalogre.Scripts.SHRUMAL_OGRE;
using MantisWarriorMonster = mantiswarrior.Scripts.MANTIS_WARRIOR;
using MantisLordMonster = mantislord.Scripts.MANTIS_LORD;
using ElderHuMonster = elderhu.Scripts.ELDER_HU;

namespace Tearscity.Scripts.EncountersFungal;

public abstract class FungalEncounterBase : ModEncounterTemplate
{
    public override bool IsValidForAct(ActModel act) => act is FungalAct;
}

public abstract class SingleFungalEncounter<TMonster> : FungalEncounterBase
    where TMonster : MonsterModel
{
    public override RoomType RoomType => Rooms.RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { ModelDb.Monster<TMonster>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new (MonsterModel, string?)[] { (ModelDb.Monster<TMonster>().ToMutable(), null) };
}

public abstract class SingleFungalWeakEncounter<TMonster> : SingleFungalEncounter<TMonster>
    where TMonster : MonsterModel
{
    public override bool IsWeak => true;
}

public abstract class SingleFungalEliteEncounter<TMonster> : SingleFungalEncounter<TMonster>
    where TMonster : MonsterModel
{
    public override RoomType RoomType => Rooms.RoomType.Elite;
}

public abstract class GroupFungalEncounter<TMonster> : FungalEncounterBase
    where TMonster : MonsterModel
{
    protected virtual int Count => 2;

    protected virtual bool Weak => false;

    public override bool IsWeak => Weak;

    public override RoomType RoomType => Rooms.RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { ModelDb.Monster<TMonster>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        Enumerable.Range(0, Count)
            .Select(_ => (ModelDb.Monster<TMonster>().ToMutable(), (string?)null))
            .ToArray();
}

public abstract class PairFungalEncounter<TFirst, TSecond> : FungalEncounterBase
    where TFirst : MonsterModel
    where TSecond : MonsterModel
{
    protected virtual bool Elite => false;

    public override RoomType RoomType => Elite ? Rooms.RoomType.Elite : Rooms.RoomType.Monster;

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
}

[RegisterActEncounter(typeof(FungalAct))]
public class FunglingPairEncounter : GroupFungalEncounter<FunglingMonster>
{
    protected override bool Weak => true;
}

[RegisterActEncounter(typeof(FungalAct))]
public class ShrumelingSwarmEncounter : GroupFungalEncounter<ShrumelingMonster>
{
    protected override int Count => 3;

    protected override bool Weak => true;
}

[RegisterActEncounter(typeof(FungalAct))]
public class AmbloomEncounter : SingleFungalWeakEncounter<AmbloomMonster>
{
}

[RegisterActEncounter(typeof(FungalAct))]
public class FungifiedHuskEncounter : SingleFungalWeakEncounter<FungifiedHuskMonster>
{
}

[RegisterActEncounter(typeof(FungalAct))]
public class FungoonEncounter : SingleFungalEncounter<FungoonMonster>
{
}

[RegisterActEncounter(typeof(FungalAct))]
public class SporgEncounter : SingleFungalEncounter<SporgMonster>
{
}

[RegisterActEncounter(typeof(FungalAct))]
public class ShrumalWarriorEncounter : SingleFungalEncounter<ShrumalWarriorMonster>
{
    public override IReadOnlyList<string> Slots => new[] { "warrior", "s1", "s2", "s3" };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new (MonsterModel, string?)[] { (ModelDb.Monster<ShrumalWarriorMonster>().ToMutable(), "warrior") };

    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://scenes/encounters/tearscity_encounter_shrumal_warrior_encounter.tscn"
    );
}

[RegisterActEncounter(typeof(FungalAct))]
public class MantisYouthEncounter : SingleFungalEncounter<MantisYouthMonster>
{
}

[RegisterActEncounter(typeof(FungalAct))]
public class FunglingPackEncounter : PairFungalEncounter<FunglingMonster, SporgMonster>
{
}

[RegisterActEncounter(typeof(FungalAct))]
public class ShrumalDuoEncounter : GroupFungalEncounter<ShrumalWarriorMonster>
{
    public override IReadOnlyList<string> Slots => new[] { "warrior1", "warrior2", "s1", "s2", "s3" };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<ShrumalWarriorMonster>().ToMutable(), "warrior1"),
            (ModelDb.Monster<ShrumalWarriorMonster>().ToMutable(), "warrior2"),
        };

    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://scenes/encounters/tearscity_encounter_shrumal_duo_encounter.tscn"
    );
}

[RegisterActEncounter(typeof(FungalAct))]
public class MantisYouthDuoEncounter : GroupFungalEncounter<MantisYouthMonster>
{
}

[RegisterActEncounter(typeof(FungalAct))]
public class AmbloomFunglingEncounter : PairFungalEncounter<AmbloomMonster, FunglingMonster>
{
}

[RegisterActEncounter(typeof(FungalAct))]
public class SporgDuoEncounter : GroupFungalEncounter<SporgMonster>
{
}

[RegisterActEncounter(typeof(FungalAct))]
public class FungoonPairEncounter : GroupFungalEncounter<FungoonMonster>
{
}

[RegisterActEncounter(typeof(FungalAct))]
public class ShrumalOgreEliteEncounter : SingleFungalEliteEncounter<ShrumalOgreMonster>
{
    public override IReadOnlyList<string> Slots => new[] { "ogre", "s1", "s2", "s3" };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new (MonsterModel, string?)[] { (ModelDb.Monster<ShrumalOgreMonster>().ToMutable(), "ogre") };

    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://scenes/encounters/tearscity_encounter_shrumal_ogre_elite_encounter.tscn"
    );
}

[RegisterActEncounter(typeof(FungalAct))]
public class MantisWarriorEliteEncounter : SingleFungalEliteEncounter<MantisWarriorMonster>
{
}

[RegisterActEncounter(typeof(FungalAct))]
public class MantisPatrolEliteEncounter : PairFungalEncounter<MantisYouthMonster, ShrumalWarriorMonster>
{
    protected override bool Elite => true;

    public override IReadOnlyList<string> Slots => new[] { "youth", "warrior", "s1", "s2" };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<MantisYouthMonster>().ToMutable(), "youth"),
            (ModelDb.Monster<ShrumalWarriorMonster>().ToMutable(), "warrior"),
        };

    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://scenes/encounters/tearscity_encounter_mantis_patrol_elite_encounter.tscn"
    );
}

[RegisterActEncounter(typeof(FungalAct))]
public class MantisLordsBossEncounter : FungalEncounterBase
{
    public override RoomType RoomType => Rooms.RoomType.Boss;

    public override IReadOnlyList<string> Slots => new[] { "lord", "twin1", "twin2" };

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { ModelDb.Monster<MantisLordMonster>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new (MonsterModel, string?)[] { (ModelDb.Monster<MantisLordMonster>().ToMutable(), "lord") };

    public override string BossNodePath => "res://Tearscity/images/map/tearscity_encounter_mantislords_boss_encounter_nodeicon";

    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://scenes/encounters/tearscity_encounter_mantislords_boss_encounter.tscn",
        MapNodeAssetPaths: new[]
        {
            "res://Tearscity/images/map/tearscity_encounter_mantislords_boss_encounter_nodeicon.png",
            "res://Tearscity/images/map/tearscity_encounter_mantislords_boss_encounter_nodeicon_outline.png",
        },
        RunHistoryIconPath: "res://Tearscity/images/map/tearscity_encounter_mantislords_boss_encounter_icon.png",
        RunHistoryIconOutlinePath: "res://Tearscity/images/map/tearscity_encounter_mantislords_boss_encounter_icon_outline.png");
}

[RegisterActEncounter(typeof(FungalAct))]
public class ElderHuBossEncounter : FungalEncounterBase
{
    public override RoomType RoomType => Rooms.RoomType.Boss;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { ModelDb.Monster<ElderHuMonster>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new (MonsterModel, string?)[] { (ModelDb.Monster<ElderHuMonster>().ToMutable(), (string?)null) };

    public override string BossNodePath => "res://Tearscity/images/map/tearscity_encounter_elder_hu_boss_encounter_nodeicon";

    public override EncounterAssetProfile AssetProfile => new(
        MapNodeAssetPaths: new[]
        {
            "res://Tearscity/images/map/tearscity_encounter_elder_hu_boss_encounter_nodeicon.png",
            "res://Tearscity/images/map/tearscity_encounter_elder_hu_boss_encounter_nodeicon_outline.png",
        },
        RunHistoryIconPath: "res://Tearscity/images/map/tearscity_encounter_elder_hu_boss_encounter_icon.png",
        RunHistoryIconOutlinePath: "res://Tearscity/images/map/tearscity_encounter_elder_hu_boss_encounter_icon_outline.png");
}
