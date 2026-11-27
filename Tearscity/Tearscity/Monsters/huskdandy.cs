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

namespace huskdandy.Scripts;

[RegisterMonster]
public class HUSK_DANDY : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 44, 39);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 49, 44);

    private int CaneDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

    private int PoseBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    private int PoseStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    private int BowDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 6);

    private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_huskdandy.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var cane = new MoveState(
            "CANE_SWIPE",
            CaneMove,
            new MultiAttackIntent(CaneDamage, 2));

        var pose = new MoveState(
            "STRIKE_A_POSE",
            PoseMove,
            new DefendIntent(),
            new BuffIntent());

        var bow = new MoveState(
            "BOW",
            BowMove,
            new SingleAttackIntent(BowDamage),
            new DebuffIntent(false));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(cane, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(pose, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(bow, MoveRepeatType.CannotRepeat, 1f);

        cane.FollowUpState = rand;
        pose.FollowUpState = rand;
        bow.FollowUpState = rand;

        return new MonsterMoveStateMachine([cane, pose, bow, rand], cane);
    }

    private async Task CaneMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(CaneDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task PoseMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, PoseBlock, ValueProp.Move, null);

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            PoseStrength,
            Creature,
            null,
            false);
    }

    private async Task BowMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(BowDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await PowerCmd.Apply<WeakPower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            WeakAmount,
            Creature,
            null,
            false);
    }
}
