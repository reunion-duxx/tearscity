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

namespace volatilegruzzer.Scripts;

[RegisterMonster]
public class VOLATILE_GRUZZER : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 160, 145);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 170, 155);

    private int BlastDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 18);

    private int DriftBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

    private int FrailAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 1, 1);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_volatilegruzzer.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var drift = new MoveState(
            "DRIFT",
            DriftMove,
            new DefendIntent());

        var blast = new MoveState(
            "BLAST",
            BlastMove,
            new SingleAttackIntent(BlastDamage),
            new DebuffIntent(false));

        drift.FollowUpState = blast;
        blast.FollowUpState = drift;

        return new MonsterMoveStateMachine([drift, blast], drift);
    }

    private async Task DriftMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, DriftBlock, ValueProp.Move, null);
    }

    private async Task BlastMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(BlastDamage)
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
}
