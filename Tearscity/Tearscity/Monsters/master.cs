using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
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

namespace master.Scripts;

[RegisterMonster]
public class MASTER : ModMonsterTemplate
{
    private bool _reviving;
    private bool _reviveUsed;
    private MoveState _respawnState = null!;
    private Tween? _fadeTween;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 182, 169);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 195, 182);

    private int ReviveHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 70, 60);

    private int DiveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 15);

    private int OrbDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

    private int StrengthGain => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    private int Dive2Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 18);

    private int StormDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_master.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    public override Task BeforeDeath(Creature creature)
    {
        if (creature == Creature && !_reviveUsed)
        {
            BeginRevive();
        }

        return Task.CompletedTask;
    }

    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature == Creature && !_reviveUsed)
        {
            BeginRevive();
        }

        return Task.CompletedTask;
    }

    public override bool ShouldStopCombatFromEnding() => _reviving;

    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature) => creature != Creature || !_reviving;

    public override bool ShouldAllowHitting(Creature creature) => creature != Creature || !_reviving;

    private void BeginRevive()
    {
        _reviveUsed = true;
        _reviving = true;
        SetMoveImmediate(_respawnState, true);
        SetStaggerVisual(true);
    }

    private void SetStaggerVisual(bool staggered)
    {
        var visuals = Creature?.GetCreatureNode()?.Visuals;
        if (visuals == null)
        {
            return;
        }

        var idle = visuals.GetNodeOrNull<CanvasItem>("Visuals");
        var stagger = visuals.GetNodeOrNull<CanvasItem>("VisualsStagger");
        if (idle == null || stagger == null)
        {
            return;
        }

        _fadeTween?.Kill();
        _fadeTween = visuals.CreateTween();
        _fadeTween.SetParallel(true);
        _fadeTween.TweenProperty(idle, "modulate:a", staggered ? 0f : 1f, 0.35f);
        _fadeTween.TweenProperty(stagger, "modulate:a", staggered ? 1f : 0f, 0.35f);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var dive = new MoveState(
            "DIVE",
            DiveMove,
            new SingleAttackIntent(DiveDamage));

        var orb = new MoveState(
            "SOUL_ORB",
            OrbMove,
            new MultiAttackIntent(OrbDamage, 2));

        var focus = new MoveState(
            "FOCUS",
            FocusMove,
            new BuffIntent(),
            new DebuffIntent(false));

        _respawnState = new MoveState(
            "RESPAWN",
            RespawnMove,
            new HealIntent());

        var dive2 = new MoveState(
            "DIVE_2",
            Dive2Move,
            new SingleAttackIntent(Dive2Damage));

        var storm = new MoveState(
            "SOUL_STORM",
            StormMove,
            new MultiAttackIntent(StormDamage, 3));

        var phase1 = new RandomBranchState("RAND_PHASE1");
        phase1.AddBranch(dive, MoveRepeatType.CannotRepeat, 2f);
        phase1.AddBranch(orb, MoveRepeatType.CannotRepeat, 2f);
        phase1.AddBranch(focus, MoveRepeatType.CannotRepeat, 1f);

        var phase2 = new RandomBranchState("RAND_PHASE2");
        phase2.AddBranch(dive2, MoveRepeatType.CannotRepeat, 1f);
        phase2.AddBranch(storm, MoveRepeatType.CannotRepeat, 1f);

        dive.FollowUpState = orb;
        orb.FollowUpState = phase1;
        focus.FollowUpState = phase1;
        dive2.FollowUpState = phase2;
        storm.FollowUpState = phase2;
        _respawnState.FollowUpState = phase2;

        return new MonsterMoveStateMachine(
            [dive, orb, focus, _respawnState, dive2, storm, phase1, phase2],
            dive);
    }

    private async Task DiveMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(DiveDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task OrbMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(OrbDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task FocusMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            StrengthGain,
            Creature,
            null,
            false);

        await PowerCmd.Apply<WeakPower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            WeakAmount,
            Creature,
            null,
            false);
    }

    private async Task RespawnMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.SetMaxHp(Creature, ReviveHp);
        await CreatureCmd.Heal(Creature, ReviveHp, true);
        _reviving = false;
        SetStaggerVisual(false);
    }

    private async Task Dive2Move(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(Dive2Damage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task StormMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(StormDamage)
            .WithHitCount(3)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }
}
