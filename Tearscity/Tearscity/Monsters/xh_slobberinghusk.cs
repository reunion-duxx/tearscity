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

namespace slobberinghusk.Scripts;

[RegisterMonster]
public class SLOBBERING_HUSK : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 190, 175);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 200, 185);

    private int LeapDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 22, 20);

    private int GnawDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);

    private int PukeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 12);

    private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 2);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_slobberinghusk.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var leap = new MoveState(
            "LEAP",
            LeapMove,
            new SingleAttackIntent(LeapDamage));

        var gnaw = new MoveState(
            "GNAW",
            GnawMove,
            new MultiAttackIntent(GnawDamage, 2));

        var puke = new MoveState(
            "PUKE",
            PukeMove,
            new SingleAttackIntent(PukeDamage),
            new DebuffIntent(false));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(leap, MoveRepeatType.CanRepeatForever, 2f);
        rand.AddBranch(gnaw, MoveRepeatType.CanRepeatForever, 2f);
        rand.AddBranch(puke, MoveRepeatType.CannotRepeat, 1f);

        leap.FollowUpState = rand;
        gnaw.FollowUpState = rand;
        puke.FollowUpState = rand;

        return new MonsterMoveStateMachine([leap, gnaw, puke, rand], leap);
    }

    private async Task LeapMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(LeapDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task GnawMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(GnawDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task PukeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(PukeDamage)
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
