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

namespace soul.Scripts;

[RegisterMonster]
public class SOUL : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 153, 133);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 169, 155);

 
    private int BasicDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 16);

    private int BasicBlock => 8;

    private int StrengthGain => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    private int SmallDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 2);
    
    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_soul.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
       
        var basicAttack = new MoveState(
            "BASIC_ATTACK",
            BasicAttackMove,
            new SingleAttackIntent(BasicDamage),
            new DefendIntent());

        
        var powerUp = new MoveState(
            "TP",
            TPMove,
            new BuffIntent());

       
        var curse = new MoveState(
            "CURSE",
            CurseMove,
            new DebuffIntent(false));

        
        var smallAttack = new MoveState(
            "SMALL_ATTACK",
            SmallAttackMove,
            new SingleAttackIntent(SmallDamage));

        
        var rand = new RandomBranchState("RAND");
        rand.AddBranch(smallAttack, MoveRepeatType.CannotRepeat, 5f); // 权重最高
        
        rand.AddBranch(curse, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(powerUp, MoveRepeatType.CannotRepeat, 3f); // 权重最低

        basicAttack.FollowUpState = rand;
        smallAttack.FollowUpState = rand;
        curse.FollowUpState = rand;
        powerUp.FollowUpState = rand;

      
        return new MonsterMoveStateMachine(
            [basicAttack, powerUp, curse, smallAttack, rand],
            basicAttack);
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

    private async Task TPMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            StrengthGain,
            Creature,
            null,
            false);
    }

    private async Task CurseMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<WeakPower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            WeakAmount,
            Creature,
            null,
            false);
    }

    private async Task SmallAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SmallDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }
}
