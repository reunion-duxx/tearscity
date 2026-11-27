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

namespace hunksentry.Scripts;


[RegisterMonster]
public class hunksentry : ModMonsterTemplate
{
    
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 32, 22);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 42, 30);
  
    private int BasicDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);
    private int BasicBlock => 5;

    
    private int HeavyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);

    
    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_hunksentry.tscn"
    );

   
    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
    
        var basicAttack = new MoveState(
            "BASIC_ATTACK", 
            BasicAttackMove, 
                             
            new SingleAttackIntent(BasicDamage),
            new DefendIntent()
        );

        
        var heavyAttack = new MoveState(
            "HEAVY_ATTACK",
            async targets => await DamageCmd 
                .Attack(HeavyDamage)
                .FromMonster(this)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null),
            new SingleAttackIntent(HeavyDamage)
        );

      

     
        basicAttack.FollowUpState = heavyAttack;
        heavyAttack.FollowUpState = basicAttack;

        
        return new MonsterMoveStateMachine([basicAttack, heavyAttack], basicAttack);
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
}
