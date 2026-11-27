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

namespace fungling.Scripts;

[RegisterMonster]
public class FUNGLING : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 39, 37);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 44, 41);

    private int NibbleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

    private int HopDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    private int WeakAmount => 1;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_fungling.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var nibble = new MoveState(
            "NIBBLE",
            NibbleMove,
            new SingleAttackIntent(NibbleDamage));

        var hop = new MoveState(
            "HOP",
            HopMove,
            new SingleAttackIntent(HopDamage),
            new DebuffIntent(false));

        nibble.FollowUpState = hop;
        hop.FollowUpState = nibble;

        return new MonsterMoveStateMachine([nibble, hop], nibble);
    }

    private async Task NibbleMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(NibbleDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task HopMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(HopDamage)
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
}
