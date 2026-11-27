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

namespace sporg.Scripts;

[RegisterMonster]
public class SPORG : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 66, 61);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 76, 71);

    private int DriftBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

    private int DriftStrength => 1;

    private int BurstDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);

    private int FrailAmount => 1;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_sporg.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var drift = new MoveState(
            "DRIFT",
            DriftMove,
            new DefendIntent(),
            new BuffIntent());

        var burst = new MoveState(
            "BURST",
            BurstMove,
            new SingleAttackIntent(BurstDamage),
            new DebuffIntent(false));

        drift.FollowUpState = burst;
        burst.FollowUpState = drift;

        return new MonsterMoveStateMachine([drift, burst], drift);
    }

    private async Task DriftMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, DriftBlock, ValueProp.Move, null);

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            DriftStrength,
            Creature,
            null,
            false);
    }

    private async Task BurstMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(BurstDamage)
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
