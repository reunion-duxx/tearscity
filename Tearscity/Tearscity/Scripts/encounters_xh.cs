using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Rooms = MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using XhAct = Tearscity.Scripts.Acts.Infectedcrossroads;
using TiktikMonster = tiktik.Scripts.TIKTIK;
using InfectedBlobMonster = infectedblob.Scripts.INFECTED_BLOB;
using FuriousVengeflyMonster = furiousvengefly.Scripts.FURIOUS_VENGEFLY;
using AspidHatchlingMonster = aspidhatchling.Scripts.ASPID_HATCHLING;
using ViolentHuskMonster = violenthusk.Scripts.VIOLENT_HUSK;
using SlobberingHuskMonster = slobberinghusk.Scripts.SLOBBERING_HUSK;
using AspidHunterMonster = aspidhunter.Scripts.ASPID_HUNTER;
using AspidMotherMonster = aspidmother.Scripts.ASPID_MOTHER;
using VolatileGruzzerMonster = volatilegruzzer.Scripts.VOLATILE_GRUZZER;
using VengeflyKingMonster = vengeflyking.Scripts.VENGEFLY_KING;
using HuskGuardMonster = huskguard.Scripts.HUSK_GUARD;
using ViolentHuskChargedMonster = violenthuskcharged.Scripts.VIOLENT_HUSK_CHARGED;

namespace Tearscity.Scripts.EncountersXh;

public abstract class XhEncounterBase : ModEncounterTemplate
{
    public override bool IsValidForAct(ActModel act) => act is XhAct;
}

public abstract class SingleXhEncounter<TMonster> : XhEncounterBase
    where TMonster : MonsterModel
{
    public override RoomType RoomType => Rooms.RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { ModelDb.Monster<TMonster>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new (MonsterModel, string?)[] { (ModelDb.Monster<TMonster>().ToMutable(), null) };
}

public abstract class SingleXhWeakEncounter<TMonster> : SingleXhEncounter<TMonster>
    where TMonster : MonsterModel
{
    public override bool IsWeak => true;
}

public abstract class SingleXhEliteEncounter<TMonster> : SingleXhEncounter<TMonster>
    where TMonster : MonsterModel
{
    public override RoomType RoomType => Rooms.RoomType.Elite;
}

public abstract class GroupXhEncounter<TMonster> : XhEncounterBase
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

public abstract class PairXhEncounter<TFirst, TSecond> : XhEncounterBase
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
}

[RegisterActEncounter(typeof(XhAct))]
public class TiktikPairEncounter : GroupXhEncounter<TiktikMonster>
{
    protected override bool Weak => true;
}

[RegisterActEncounter(typeof(XhAct))]
public class InfectedBlobSwarmEncounter : GroupXhEncounter<InfectedBlobMonster>
{
    protected override int Count => 3;

    protected override bool Weak => true;
}

[RegisterActEncounter(typeof(XhAct))]
public class FuriousVengeflyDuoEncounter : GroupXhEncounter<FuriousVengeflyMonster>
{
    protected override bool Weak => true;
}

[RegisterActEncounter(typeof(XhAct))]
public class HatchlingSwarmEncounter : GroupXhEncounter<AspidHatchlingMonster>
{
    protected override int Count => 3;

    protected override bool Weak => true;
}

[RegisterActEncounter(typeof(XhAct))]
public class ViolentHuskEncounter : SingleXhEncounter<ViolentHuskMonster>
{
}

[RegisterActEncounter(typeof(XhAct))]
public class SlobberingHuskEncounter : SingleXhEncounter<SlobberingHuskMonster>
{
}

[RegisterActEncounter(typeof(XhAct))]
public class AspidHunterEncounter : SingleXhEncounter<AspidHunterMonster>
{
}

[RegisterActEncounter(typeof(XhAct))]
public class VolatileGruzzerEncounter : SingleXhEncounter<VolatileGruzzerMonster>
{
}

[RegisterActEncounter(typeof(XhAct))]
public class AspidMotherEncounter : SingleXhEncounter<AspidMotherMonster>
{
    public override IReadOnlyList<string> Slots => new[] { "mother", "s1", "s2" };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new (MonsterModel, string?)[] { (ModelDb.Monster<AspidMotherMonster>().ToMutable(), "mother") };

    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://scenes/encounters/tearscity_encounter_aspid_mother_encounter.tscn"
    );
}

[RegisterActEncounter(typeof(XhAct))]
public class FuriousVengeflyPackEncounter : PairXhEncounter<FuriousVengeflyMonster, VolatileGruzzerMonster>
{
}

[RegisterActEncounter(typeof(XhAct))]
public class HusksPairEncounter : PairXhEncounter<ViolentHuskMonster, SlobberingHuskMonster>
{
}

[RegisterActEncounter(typeof(XhAct))]
public class TiktikBlobPairEncounter : PairXhEncounter<TiktikMonster, InfectedBlobMonster>
{
}

[RegisterActEncounter(typeof(XhAct))]
public class VengeflyKingEliteEncounter : SingleXhEliteEncounter<VengeflyKingMonster>
{
}

[RegisterActEncounter(typeof(XhAct))]
public class HuskGuardEliteEncounter : SingleXhEliteEncounter<HuskGuardMonster>
{
}

[RegisterActEncounter(typeof(XhAct))]
public class ViolentHuskChargedEliteEncounter : SingleXhEliteEncounter<ViolentHuskChargedMonster>
{
}
