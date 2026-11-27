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

namespace mantisyouth.Scripts;

[RegisterMonster]
public class MANTIS_YOUTH : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 76, 71);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 85, 81);

    private int ScytheDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

    private int DiveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 13);

    private int VulnerableAmount => 1;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_mantisyouth.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var scythe = new MoveState(
            "SCYTHE",
            ScytheMove,
            new MultiAttackIntent(ScytheDamage, 2));

        var dive = new MoveState(
            "DIVEBOMB",
            DiveMove,
            new SingleAttackIntent(DiveDamage),
            new DebuffIntent(false));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(scythe, MoveRepeatType.CanRepeatForever, 2f);
        rand.AddBranch(dive, MoveRepeatType.CannotRepeat, 2f);

        scythe.FollowUpState = rand;
        dive.FollowUpState = rand;

        return new MonsterMoveStateMachine([scythe, dive, rand], scythe);
    }

    private async Task ScytheMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(ScytheDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task DiveMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(DiveDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await PowerCmd.Apply<VulnerablePower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            VulnerableAmount,
            Creature,
            null,
            false);
    }
}
