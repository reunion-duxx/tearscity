using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

namespace mantislordtwin.Scripts;

[RegisterMonster]
public class MANTIS_LORD_TWIN : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 158, 150);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 173, 165);

    private int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);

    private int DropDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);

    private int VolleyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

    private int GuardBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

    private int VulnerableAmount => 1;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_mantislordtwin.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var slash = new MoveState(
            "TWIN_SLASH",
            SlashMove,
            new MultiAttackIntent(SlashDamage, 2));

        var drop = new MoveState(
            "CEILING_DROP",
            DropMove,
            new SingleAttackIntent(DropDamage),
            new DebuffIntent(false));

        var volley = new MoveState(
            "SCYTHE_VOLLEY",
            VolleyMove,
            new MultiAttackIntent(VolleyDamage, 3));

        var guard = new MoveState(
            "GUARD_STANCE",
            GuardMove,
            new DefendIntent());

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(slash, MoveRepeatType.CanRepeatForever, 3f);
        rand.AddBranch(volley, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(drop, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(guard, MoveRepeatType.CannotRepeat, 1f);

        slash.FollowUpState = rand;
        drop.FollowUpState = rand;
        volley.FollowUpState = rand;
        guard.FollowUpState = rand;

        return new MonsterMoveStateMachine([slash, drop, volley, guard, rand], rand);
    }

    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SlashDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task DropMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(DropDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await PowerCmd.Apply<VulnerablePower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            VulnerableAmount,
            Creature,
            null,
            false);
    }

    private async Task VolleyMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(VolleyDamage)
            .WithHitCount(3)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task GuardMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, GuardBlock, ValueProp.Move, null);
    }
}
