using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace mistake.Scripts;

[RegisterMonster]
public class MISTAKE : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 31, 26);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 36, 31);

    private int BumpDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    private int OozeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 2);

    private int OozeBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    private int MeltStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    private int MeltBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 6);

    private const int MeltSelfDamage = 2;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_mistake.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var bump = new MoveState(
            "BUMP",
            BumpMove,
            new SingleAttackIntent(BumpDamage));

        var ooze = new MoveState(
            "OOZE",
            OozeMove,
            new MultiAttackIntent(OozeDamage, 2),
            new DefendIntent());

        var melt = new MoveState(
            "MELT",
            MeltMove,
            new BuffIntent(),
            new DefendIntent());

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(bump, MoveRepeatType.CannotRepeat, 3f);
        rand.AddBranch(ooze, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(melt, MoveRepeatType.CannotRepeat, 1f);

        bump.FollowUpState = rand;
        ooze.FollowUpState = rand;
        melt.FollowUpState = rand;

        return new MonsterMoveStateMachine([bump, ooze, melt, rand], bump);
    }

    private async Task BumpMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(BumpDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task OozeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(OozeDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await CreatureCmd.GainBlock(Creature, OozeBlock, ValueProp.Move, null);
    }

    private async Task MeltMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            MeltStrength,
            Creature,
            null,
            false);

        await CreatureCmd.GainBlock(Creature, MeltBlock, ValueProp.Move, null);

        if (Creature.CurrentHp > MeltSelfDamage + 1)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                Creature,
                MeltSelfDamage,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null,
                null);
        }
    }
}
