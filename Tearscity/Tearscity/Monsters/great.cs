using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace great.Scripts;

[RegisterMonster]
public class GREAT : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 117, 103);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 124, 108);

    private int BasicDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 11);

    private int BlockAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 6);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_great.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var attack = new MoveState(
            "BASIC_ATTACK",
            AttackMove,
            new SingleAttackIntent(BasicDamage));

        var defend = new MoveState(
            "DEFEND",
            DefendMove,
            new DefendIntent());

        attack.FollowUpState = defend;
        defend.FollowUpState = attack;

        return new MonsterMoveStateMachine([attack, defend], attack);
    }

    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(BasicDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task DefendMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, BlockAmount, ValueProp.Move, null);
    }
}
