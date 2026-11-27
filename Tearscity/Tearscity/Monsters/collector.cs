using System;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;
using FollyMonster = folly.Scripts.folly;

namespace collector.Scripts;

[RegisterMonster]
public class COLLECTOR : ModMonsterTemplate
{
    private bool _phase2;
    private int _jarStrengthStacks;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 190, 177);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 200, 187);

    private int ClawDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

    private int TossDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

    private int CollectBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 7);

    private int VulnerableAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    private int SlamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 11);

    private int RageDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 12);

    private int RageStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    private int StormDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

    private int JarDeathStrength => 1;

    private int JarStrengthCap => 3;

    private int MaxJars => _phase2 ? 2 : 1;

    private bool IsHalfHp => Creature != null && Creature.MaxHp > 0 && Creature.CurrentHp * 2 <= Creature.MaxHp;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_collector.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var claw = new MoveState(
            "CLAW",
            ClawMove,
            new SingleAttackIntent(ClawDamage));

        var jarToss = new MoveState(
            "JAR_TOSS",
            JarTossMove,
            new MultiAttackIntent(TossDamage, 2));

        var collect = new MoveState(
            "COLLECT",
            CollectMove,
            new DefendIntent(),
            new DebuffIntent(false));

        var release = new MoveState(
            "RELEASE",
            ReleaseMove,
            new SummonIntent());

        var jarSlam = new MoveState(
            "JAR_SLAM",
            JarSlamMove,
            new SingleAttackIntent(SlamDamage));

        var rage = new MoveState(
            "RAGE",
            RageMove,
            new SingleAttackIntent(RageDamage),
            new BuffIntent());

        var storm = new MoveState(
            "STORM",
            StormMove,
            new MultiAttackIntent(StormDamage, 3));

        var summon = new ConditionalBranchState("SUMMON_SEL");
        summon.AddState(release, CanSummon);
        summon.AddState(jarSlam, () => true);

        var phase1 = new RandomBranchState("RAND_PHASE1");
        phase1.AddBranch(claw, MoveRepeatType.CannotRepeat, 3f);
        phase1.AddBranch(jarToss, MoveRepeatType.CannotRepeat, 2f);
        phase1.AddBranch(summon, MoveRepeatType.CannotRepeat, 2f);
        phase1.AddBranch(collect, MoveRepeatType.CannotRepeat, 1f);

        var phase2 = new RandomBranchState("RAND_PHASE2");
        phase2.AddBranch(claw, MoveRepeatType.CannotRepeat, 2f);
        phase2.AddBranch(storm, MoveRepeatType.CannotRepeat, 2f);
        phase2.AddBranch(summon, MoveRepeatType.CannotRepeat, 2f);
        phase2.AddBranch(collect, MoveRepeatType.CannotRepeat, 1f);

        var next = new ConditionalBranchState("NEXT");
        next.AddState(rage, () => !_phase2 && IsHalfHp);
        next.AddState(phase2, () => _phase2);
        next.AddState(phase1, () => true);

        claw.FollowUpState = next;
        jarToss.FollowUpState = next;
        collect.FollowUpState = next;
        release.FollowUpState = next;
        jarSlam.FollowUpState = next;
        rage.FollowUpState = next;
        storm.FollowUpState = next;

        return new MonsterMoveStateMachine(
            [claw, jarToss, collect, release, jarSlam, rage, storm, summon, phase1, phase2, next],
            next);
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature == Creature)
        {
            await ClearJars();
            return;
        }

        if (creature.Monster is not FollyMonster)
        {
            return;
        }

        if (!Creature.IsAlive || _jarStrengthStacks >= JarStrengthCap)
        {
            return;
        }

        _jarStrengthStacks++;

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            JarDeathStrength,
            Creature,
            null,
            false);
    }

    private bool CanSummon()
    {
        var combatState = CombatState;
        var encounter = combatState?.Encounter;
        if (combatState == null || encounter == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(encounter.GetNextSlot(combatState)))
        {
            return false;
        }

        var jars = combatState.Enemies.Count(c => c.IsAlive && c.Monster is FollyMonster);
        return jars < MaxJars;
    }

    private async Task ClearJars()
    {
        var combatState = CombatState;
        if (combatState == null || !combatState.IsLiveCombat())
        {
            return;
        }

        var jars = combatState.Enemies
            .Where(c => c != Creature && c.IsAlive && c.Monster is FollyMonster)
            .ToList();

        if (jars.Count == 0)
        {
            return;
        }

        await CreatureCmd.Kill(jars, true);
    }

    private async Task ClawMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(ClawDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task JarTossMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(TossDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task CollectMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, CollectBlock, ValueProp.Move, null);

        await PowerCmd.Apply<VulnerablePower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            VulnerableAmount,
            Creature,
            null,
            false);
    }

    private async Task ReleaseMove(IReadOnlyList<Creature> targets)
    {
        var combatState = CombatState;
        var encounter = combatState?.Encounter;
        if (combatState == null || encounter == null || !combatState.IsLiveCombat())
        {
            return;
        }

        var slot = encounter.GetNextSlot(combatState);
        if (string.IsNullOrEmpty(slot))
        {
            return;
        }

        await Cmd.Wait(0.4f, false);
        var jar = await CreatureCmd.Add<FollyMonster>(combatState, slot);
        Shatter(jar);
        FadeIn(jar);
    }

    private static void Shatter(Creature creature)
    {
        try
        {
            var container = NCombatRoom.Instance?.CombatVfxContainer;
            var scene = ResourceLoader.Load<PackedScene>("res://scenes/vfx/vfx_rock_shatter.tscn");
            if (container == null || scene == null)
            {
                return;
            }

            var node = scene.Instantiate<Node2D>();
            container.AddChild(node);

            var target = creature.GetCreatureNode();
            node.GlobalPosition = target != null && GodotObject.IsInstanceValid(target)
                ? target.GlobalPosition
                : Vector2.Zero;
        }
        catch (Exception e)
        {
            Log.Warn("[Tearscity] shatter vfx failed: " + e.Message, 2);
        }
    }

    private static void FadeIn(Creature creature)
    {
        try
        {
            var node = creature.GetCreatureNode();
            if (node == null || !GodotObject.IsInstanceValid(node))
            {
                return;
            }

            var visuals = node.Visuals;
            if (visuals == null || !GodotObject.IsInstanceValid(visuals))
            {
                return;
            }

            visuals.Modulate = new Color(1f, 1f, 1f, 0f);
            var tween = visuals.CreateTween();
            tween.TweenProperty(visuals, "modulate:a", 1f, 0.35f);
        }
        catch (Exception e)
        {
            Log.Warn("[Tearscity] summon fade failed: " + e.Message, 2);
        }
    }

    private async Task JarSlamMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SlamDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task RageMove(IReadOnlyList<Creature> targets)
    {
        _phase2 = true;

        await DamageCmd
            .Attack(RageDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            RageStrength,
            Creature,
            null,
            false);
    }

    private async Task StormMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(StormDamage)
            .WithHitCount(3)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
