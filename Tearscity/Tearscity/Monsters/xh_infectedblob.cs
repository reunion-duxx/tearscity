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

namespace infectedblob.Scripts;

[RegisterMonster]
public class INFECTED_BLOB : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 66, 58);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 74, 66);

    private int InfectDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 12);

    private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 1, 1);

    private int BurstDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 15);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_infectedblob.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var infect = new MoveState(
            "INFECT",
            InfectMove,
            new SingleAttackIntent(InfectDamage),
            new DebuffIntent(false));

        var swell = new MoveState(
            "SWELL",
            SwellMove,
            new DefendIntent());

        var burst = new MoveState(
            "BURST",
            BurstMove,
            new SingleAttackIntent(BurstDamage));

        infect.FollowUpState = swell;
        swell.FollowUpState = burst;
        burst.FollowUpState = burst;

        return new MonsterMoveStateMachine([infect, swell, burst], infect);
    }

    private async Task InfectMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(InfectDamage)
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

    private async Task SwellMove(IReadOnlyList<Creature> targets)
    {
        await Task.CompletedTask;
    }

    private async Task BurstMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(BurstDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await CreatureCmd.Kill(Creature, false);
    }
}
