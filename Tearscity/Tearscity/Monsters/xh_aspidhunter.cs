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

namespace aspidhunter.Scripts;

[RegisterMonster]
public class ASPID_HUNTER : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 165, 150);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 175, 160);

    private int ShotDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

    private int AimAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 1, 1);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_aspidhunter.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var shot = new MoveState(
            "TRIPLE_SHOT",
            ShotMove,
            new MultiAttackIntent(ShotDamage, 3));

        var aim = new MoveState(
            "AIM",
            AimMove,
            new DebuffIntent(false));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(shot, MoveRepeatType.CanRepeatForever, 3f);
        rand.AddBranch(aim, MoveRepeatType.CannotRepeat, 1f);

        shot.FollowUpState = rand;
        aim.FollowUpState = rand;

        return new MonsterMoveStateMachine([shot, aim, rand], shot);
    }

    private async Task ShotMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(ShotDamage)
            .WithHitCount(3)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task AimMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<VulnerablePower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            AimAmount,
            Creature,
            null,
            false);
    }
}
