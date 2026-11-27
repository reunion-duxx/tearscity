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

namespace winged.Scripts;

[RegisterMonster]
public class WINGED : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 44, 39);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 49, 44);

    private int JabDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    private int HoverBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    private int HoverStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    private int DiveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 12);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_wingedsentry.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var jab = new MoveState(
            "JAB",
            JabMove,
            new MultiAttackIntent(JabDamage, 2));

        var hover = new MoveState(
            "HOVER",
            HoverMove,
            new DefendIntent(),
            new BuffIntent());

        var dive = new MoveState(
            "DIVE",
            DiveMove,
            new SingleAttackIntent(DiveDamage));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(jab, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(hover, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(dive, MoveRepeatType.CannotRepeat, 2f);

        jab.FollowUpState = rand;
        hover.FollowUpState = rand;
        dive.FollowUpState = rand;

        return new MonsterMoveStateMachine([jab, hover, dive, rand], jab);
    }

    private async Task JabMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(JabDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task HoverMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, HoverBlock, ValueProp.Move, null);

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            HoverStrength,
            Creature,
            null,
            false);
    }

    private async Task DiveMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(DiveDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }
}
