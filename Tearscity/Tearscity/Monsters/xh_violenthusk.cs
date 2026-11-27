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

namespace violenthusk.Scripts;

[RegisterMonster]
public class VIOLENT_HUSK : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 180, 165);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 190, 175);

    private int ChargeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 18);

    private int CrashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

    private int RageBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    private int RageStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 2);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_violenthusk.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var charge = new MoveState(
            "CHARGE",
            ChargeMove,
            new SingleAttackIntent(ChargeDamage));

        var crash = new MoveState(
            "CRASH",
            CrashMove,
            new MultiAttackIntent(CrashDamage, 2));

        var rage = new MoveState(
            "RAGE",
            RageMove,
            new DefendIntent(),
            new BuffIntent());

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(charge, MoveRepeatType.CanRepeatForever, 3f);
        rand.AddBranch(crash, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(rage, MoveRepeatType.CannotRepeat, 1f);

        charge.FollowUpState = rand;
        crash.FollowUpState = rand;
        rage.FollowUpState = rand;

        return new MonsterMoveStateMachine([charge, crash, rage, rand], charge);
    }

    private async Task ChargeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(ChargeDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task CrashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(CrashDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task RageMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, RageBlock, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            RageStrength,
            Creature,
            null,
            false);
    }
}
