using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace folly.Scripts;

[RegisterMonster]
public class folly : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 26, 22);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 30, 26);

    // ---- 数值（都可以自己调）----
    private int BasicDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 5);

    private int BasicBlock => 5;

    private int BlowDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 8);

    private int VulnerableAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    
    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_folly.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
      
        var basicAttack = new MoveState(
            "BASIC_ATTACK",
            BasicAttackMove,
            new SingleAttackIntent(BasicDamage),
            new DefendIntent());

       
        var blow = new MoveState(
            "BLOW",
            BlowMove,
            new SingleAttackIntent(BlowDamage),
            new DebuffIntent(false));

        basicAttack.FollowUpState = blow;
        blow.FollowUpState = basicAttack;

        return new MonsterMoveStateMachine([basicAttack, blow], basicAttack);
    }

    private async Task BasicAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(BasicDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

       
        await CreatureCmd.GainBlock(Creature, BasicBlock, ValueProp.Move, null);
    }

    private async Task BlowMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(BlowDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
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
