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

namespace fungifiedhusk.Scripts;

[RegisterMonster]
public class FUNGIFIED_HUSK : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 46, 44);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 54, 51);

    private int LungeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

    private int SwellBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

    private int BurstDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 14);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_fungifiedhusk.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var lunge = new MoveState(
            "LUNGE",
            LungeMove,
            new SingleAttackIntent(LungeDamage));

        var swell = new MoveState(
            "SWELL",
            SwellMove,
            new DefendIntent());

        var burst = new MoveState(
            "BURST",
            BurstMove,
            new SingleAttackIntent(BurstDamage));

        lunge.FollowUpState = swell;
        swell.FollowUpState = burst;
        burst.FollowUpState = burst;

        return new MonsterMoveStateMachine([lunge, swell, burst], lunge);
    }

    private async Task LungeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(LungeDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task SwellMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, SwellBlock, ValueProp.Move, null);
    }

    private async Task BurstMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(BurstDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await CreatureCmd.Kill(Creature, false);
    }
}
