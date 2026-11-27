using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

namespace elderhu.Scripts;

[RegisterMonster]
public class ELDER_HU : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 396, 380);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 416, 400);

    private int RingDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

    private int SlamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 32, 30);

    private int FlashBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 28, 25);

    private int FlashStrength => 2;

    private int MeditateBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

    private int FrailAmount => 1;

    private int WeakAmount => 1;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_elderhu.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var ring = new MoveState(
            "RING_BARRAGE",
            RingMove,
            new MultiAttackIntent(RingDamage, 4));

        var slam = new MoveState(
            "SLAM",
            SlamMove,
            new SingleAttackIntent(SlamDamage),
            new DebuffIntent(false));

        var flash = new MoveState(
            "FLASH",
            FlashMove,
            new DefendIntent(),
            new BuffIntent());

        var meditate = new MoveState(
            "MEDITATE",
            MeditateMove,
            new DefendIntent(),
            new DebuffIntent(false));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(slam, MoveRepeatType.CanRepeatForever, 3f);
        rand.AddBranch(ring, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(flash, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(meditate, MoveRepeatType.UseOnlyOnce, 1f);

        slam.FollowUpState = rand;
        ring.FollowUpState = rand;
        flash.FollowUpState = rand;
        meditate.FollowUpState = rand;

        return new MonsterMoveStateMachine([slam, ring, flash, meditate, rand], rand);
    }

    private async Task RingMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(RingDamage)
            .WithHitCount(4)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task SlamMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SlamDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await PowerCmd.Apply<FrailPower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            FrailAmount,
            Creature,
            null,
            false);
    }

    private async Task FlashMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, FlashBlock, ValueProp.Move, null);

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            FlashStrength,
            Creature,
            null,
            false);
    }

    private async Task MeditateMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, MeditateBlock, ValueProp.Move, null);

        await PowerCmd.Apply<WeakPower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            WeakAmount,
            Creature,
            null,
            false);
    }
}
