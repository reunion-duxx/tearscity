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

namespace tiktik.Scripts;

[RegisterMonster]
public class TIKTIK : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 72, 64);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 80, 72);

    private int SkitterDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 11);

    private int HideBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_tiktik.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var skitter = new MoveState(
            "SKITTER",
            SkitterMove,
            new SingleAttackIntent(SkitterDamage));

        var hide = new MoveState(
            "HIDE",
            HideMove,
            new DefendIntent());

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(skitter, MoveRepeatType.CanRepeatForever, 3f);
        rand.AddBranch(hide, MoveRepeatType.CannotRepeat, 1f);

        skitter.FollowUpState = rand;
        hide.FollowUpState = rand;

        return new MonsterMoveStateMachine([skitter, hide, rand], skitter);
    }

    private async Task SkitterMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SkitterDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task HideMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, HideBlock, ValueProp.Move, null);
    }
}
