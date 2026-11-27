using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace lance.Scripts;

[RegisterMonster]
public class LANCE : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 52, 47);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 57, 52);

    private int ProdDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

    private int BraceBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

    private int BraceStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    private int LungeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 15);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_lancesentry.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var prod = new MoveState(
            "PROD",
            ProdMove,
            new SingleAttackIntent(ProdDamage));

        var brace = new MoveState(
            "BRACE",
            BraceMove,
            new DefendIntent(),
            new BuffIntent());

        var lunge = new MoveState(
            "LUNGE",
            LungeMove,
            new SingleAttackIntent(LungeDamage));

        prod.FollowUpState = brace;
        brace.FollowUpState = lunge;
        lunge.FollowUpState = prod;

        return new MonsterMoveStateMachine([prod, brace, lunge], prod);
    }

    private async Task ProdMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(ProdDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task BraceMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, BraceBlock, ValueProp.Move, null);

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            BraceStrength,
            Creature,
            null,
            false);
    }

    private async Task LungeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(LungeDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
