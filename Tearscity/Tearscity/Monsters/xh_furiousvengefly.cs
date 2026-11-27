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

namespace furiousvengefly.Scripts;

[RegisterMonster]
public class FURIOUS_VENGEFLY : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 78, 70);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 86, 78);

    private int DiveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 13);

    private int ScreechAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 1, 1);

    private int ScreechBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_furiousvengefly.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var dive = new MoveState(
            "DIVE",
            DiveMove,
            new SingleAttackIntent(DiveDamage));

        var screech = new MoveState(
            "SCREECH",
            ScreechMove,
            new DefendIntent(),
            new DebuffIntent(false));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(dive, MoveRepeatType.CanRepeatForever, 3f);
        rand.AddBranch(screech, MoveRepeatType.CannotRepeat, 1f);

        dive.FollowUpState = rand;
        screech.FollowUpState = rand;

        return new MonsterMoveStateMachine([dive, screech, rand], dive);
    }

    private async Task DiveMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(DiveDamage)
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
