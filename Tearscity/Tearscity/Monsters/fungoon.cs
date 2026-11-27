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

namespace fungoon.Scripts;

[RegisterMonster]
public class FUNGOON : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 83, 78);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 93, 88);

    private int SpitDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 11);

    private int CloudBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 9);

    private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_fungoon.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var spit = new MoveState(
            "SPIT",
            SpitMove,
            new SingleAttackIntent(SpitDamage));

        var cloud = new MoveState(
            "SPORE_CLOUD",
            CloudMove,
            new DefendIntent(),
            new DebuffIntent(false));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(spit, MoveRepeatType.CanRepeatForever, 2f);
        rand.AddBranch(cloud, MoveRepeatType.CannotRepeat, 1f);

        spit.FollowUpState = rand;
        cloud.FollowUpState = rand;

        return new MonsterMoveStateMachine([spit, cloud, rand], spit);
    }

    private async Task SpitMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SpitDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task CloudMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, CloudBlock, ValueProp.Move, null);

        await PowerCmd.Apply<WeakPower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            WeakAmount,
            Creature,
            null,
            false);
    }
}
