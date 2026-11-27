using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace mantiswarrior.Scripts;

[RegisterMonster]
public class MANTIS_WARRIOR : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 206, 201);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 218, 214);

    private int SideSliceDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 11);

    private int UpSliceDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 26, 24);

    private int DanceBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

    private int DanceStrength => 2;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_mantiswarrior.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var side = new MoveState(
            "SIDESLICE",
            SideSliceMove,
            new MultiAttackIntent(SideSliceDamage, 2));

        var up = new MoveState(
            "UPSLICE",
            UpSliceMove,
            new SingleAttackIntent(UpSliceDamage));

        var dance = new MoveState(
            "DANCE",
            DanceMove,
            new DefendIntent(),
            new BuffIntent());

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(side, MoveRepeatType.CanRepeatForever, 2f);
        rand.AddBranch(up, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(dance, MoveRepeatType.CannotRepeat, 1f);

        side.FollowUpState = rand;
        up.FollowUpState = rand;
        dance.FollowUpState = rand;

        return new MonsterMoveStateMachine([side, up, dance, rand], side);
    }

    private async Task SideSliceMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SideSliceDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task UpSliceMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(UpSliceDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task DanceMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, DanceBlock, ValueProp.Move, null);

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            DanceStrength,
            Creature,
            null,
            false);
    }
}
