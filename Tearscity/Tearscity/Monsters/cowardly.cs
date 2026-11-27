using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace cowardly.Scripts;

[RegisterMonster]
public class COWARDLY : ModMonsterTemplate
{
    private bool _cornered;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 36, 31);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 42, 36);

    private int ScurryBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 8);

    private int FlurryDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

    private int PanicDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 6);

    private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    private int CorneredDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    private int CorneredStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    private bool IsCorneredHp => Creature != null && Creature.MaxHp > 0 && Creature.CurrentHp * 2 <= Creature.MaxHp;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_cowardlyhusk.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var scurry = new MoveState(
            "SCURRY",
            ScurryMove,
            new DefendIntent());

        var flurry = new MoveState(
            "FLURRY",
            FlurryMove,
            new MultiAttackIntent(FlurryDamage, 2));

        var panic = new MoveState(
            "PANIC",
            PanicMove,
            new SingleAttackIntent(PanicDamage),
            new DebuffIntent(false));

        var cornered = new MoveState(
            "CORNERED",
            CorneredMove,
            new SingleAttackIntent(CorneredDamage),
            new BuffIntent());

        var phase1 = new RandomBranchState("RAND_PHASE1");
        phase1.AddBranch(scurry, MoveRepeatType.CannotRepeat, 2f);
        phase1.AddBranch(flurry, MoveRepeatType.CannotRepeat, 2f);
        phase1.AddBranch(panic, MoveRepeatType.CannotRepeat, 1f);

        var phase2 = new RandomBranchState("RAND_PHASE2");
        phase2.AddBranch(cornered, MoveRepeatType.CannotRepeat, 2f);
        phase2.AddBranch(flurry, MoveRepeatType.CannotRepeat, 2f);
        phase2.AddBranch(scurry, MoveRepeatType.CannotRepeat, 1f);

        var next = new ConditionalBranchState("NEXT");
        next.AddState(cornered, () => !_cornered && IsCorneredHp);
        next.AddState(phase2, () => _cornered);
        next.AddState(phase1, () => true);

        scurry.FollowUpState = next;
        flurry.FollowUpState = next;
        panic.FollowUpState = next;
        cornered.FollowUpState = next;

        return new MonsterMoveStateMachine(
            [scurry, flurry, panic, cornered, phase1, phase2, next],
            next);
    }

    private async Task ScurryMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, ScurryBlock, ValueProp.Move, null);
    }

    private async Task FlurryMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(FlurryDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task PanicMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(PanicDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await PowerCmd.Apply<WeakPower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            WeakAmount,
            Creature,
            null,
            false);
    }

    private async Task CorneredMove(IReadOnlyList<Creature> targets)
    {
        _cornered = true;

        await DamageCmd
            .Attack(CorneredDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            CorneredStrength,
            Creature,
            null,
            false);
    }
}
