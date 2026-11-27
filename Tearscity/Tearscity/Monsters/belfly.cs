using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace belfly.Scripts;

[RegisterMonster]
public class BELFLY : ModMonsterTemplate
{
    private const int FuseSelfDamage = 2;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 23, 18);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 29, 23);

    private int BuzzDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

    private int BoomDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 11);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_belfly.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var buzz = new MoveState(
            "BUZZ",
            BuzzMove,
            new SingleAttackIntent(BuzzDamage));

        var fuse = new MoveState(
            "FUSE",
            FuseMove,
            new BuffIntent());

        var boom = new MoveState(
            "BOOM",
            BoomMove,
            new SingleAttackIntent(BoomDamage));

        buzz.FollowUpState = fuse;
        fuse.FollowUpState = boom;
        boom.FollowUpState = boom;

        return new MonsterMoveStateMachine([buzz, fuse, boom], buzz);
    }

    private async Task BuzzMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(BuzzDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task FuseMove(IReadOnlyList<Creature> targets)
    {
        if (Creature.CurrentHp > FuseSelfDamage + 1)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                Creature,
                FuseSelfDamage,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null,
                null);
        }
    }

    private async Task BoomMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(BoomDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await CreatureCmd.Kill(Creature, false);
    }
}
