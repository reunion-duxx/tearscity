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

namespace ambloom.Scripts;

[RegisterMonster]
public class AMBLOOM : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 44, 41);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 51, 49);

    private int SporeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

    private int BloomBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 7);

    private int WeakAmount => 1;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_ambloom.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var spore = new MoveState(
            "SPORE_SHOT",
            SporeShotMove,
            new SingleAttackIntent(SporeDamage));

        var bloom = new MoveState(
            "BLOOM",
            BloomMove,
            new DefendIntent(),
            new DebuffIntent(false));

        spore.FollowUpState = bloom;
        bloom.FollowUpState = spore;

        return new MonsterMoveStateMachine([spore, bloom], spore);
    }

    private async Task SporeShotMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SporeDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task BloomMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, BloomBlock, ValueProp.Move, null);

        await PowerCmd.Apply<WeakPower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            WeakAmount,
            Creature,
            null,
            false);
    }
}
