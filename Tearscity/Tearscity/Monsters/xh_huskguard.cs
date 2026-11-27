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

namespace huskguard.Scripts;

[RegisterMonster]
public class HUSK_GUARD : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 360, 340);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 376, 356);

    private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 40, 38);

    private int FlopDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 22, 20);

    private int GuardBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 26, 24);

    private int GuardStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 3);

    private int FrailAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 1, 1);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_huskguard.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var smash = new MoveState(
            "SMASH",
            SmashMove,
            new SingleAttackIntent(SmashDamage));

        var flop = new MoveState(
            "FLOP",
            FlopMove,
            new SingleAttackIntent(FlopDamage),
            new DebuffIntent(false));

        var guard = new MoveState(
            "GUARD",
            GuardMove,
            new DefendIntent(),
            new BuffIntent());

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(smash, MoveRepeatType.CanRepeatForever, 3f);
        rand.AddBranch(flop, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(guard, MoveRepeatType.CannotRepeat, 1f);

        smash.FollowUpState = rand;
        flop.FollowUpState = rand;
        guard.FollowUpState = rand;

        return new MonsterMoveStateMachine([smash, flop, guard, rand], smash);
    }

    private async Task SmashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SmashDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task FlopMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(FlopDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Apply<FrailPower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            FrailAmount,
            Creature,
            null,
            false);
    }

    private async Task GuardMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, GuardBlock, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            GuardStrength,
            Creature,
            null,
            false);
    }
}
