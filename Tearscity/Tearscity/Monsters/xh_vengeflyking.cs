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

namespace vengeflyking.Scripts;

[RegisterMonster]
public class VENGEFLY_KING : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 340, 320);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 356, 336);

    private int BuzzDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 32, 30);

    private int DiveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 24, 22);

    private int ScreechBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

    private int ScreechAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 2);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_vengeflyking.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var buzz = new MoveState(
            "BUZZ",
            BuzzMove,
            new SingleAttackIntent(BuzzDamage));

        var dive = new MoveState(
            "DIVE",
            DiveMove,
            new MultiAttackIntent(DiveDamage, 2));

        var screech = new MoveState(
            "SCREECH",
            ScreechMove,
            new DefendIntent(),
            new DebuffIntent(true));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(buzz, MoveRepeatType.CanRepeatForever, 3f);
        rand.AddBranch(dive, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(screech, MoveRepeatType.CannotRepeat, 1f);

        buzz.FollowUpState = rand;
        dive.FollowUpState = rand;
        screech.FollowUpState = rand;

        return new MonsterMoveStateMachine([buzz, dive, screech, rand], buzz);
    }

    private async Task BuzzMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(BuzzDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task DiveMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(DiveDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task ScreechMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, ScreechBlock, ValueProp.Move, null);
        await PowerCmd.Apply<VulnerablePower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            ScreechAmount,
            Creature,
            null,
            false);
    }
}
