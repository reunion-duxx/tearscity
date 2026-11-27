using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
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

namespace watcher.Scripts;

[RegisterMonster]
public class WATCHER : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 81, 70);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 91, 81);

    private int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    private int TwinDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

    private int RollDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 11);

    private int RollBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 5);

    private int BashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

    private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    private int RageStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_watcher.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var slash = new MoveState(
            "SLASH",
            SlashMove,
            new SingleAttackIntent(SlashDamage));

        var twinSlash = new MoveState(
            "TWIN_SLASH",
            TwinSlashMove,
            new MultiAttackIntent(TwinDamage, 2));

        var roll = new MoveState(
            "ROLL",
            RollMove,
            new SingleAttackIntent(RollDamage),
            new DefendIntent());

        var shieldBash = new MoveState(
            "SHIELD_BASH",
            ShieldBashMove,
            new SingleAttackIntent(BashDamage),
            new DebuffIntent(false));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(slash, MoveRepeatType.CannotRepeat, 3f);
        rand.AddBranch(twinSlash, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(roll, MoveRepeatType.CannotRepeat, 1f);
        rand.AddBranch(shieldBash, MoveRepeatType.CannotRepeat, 1f);

        slash.FollowUpState = rand;
        twinSlash.FollowUpState = rand;
        roll.FollowUpState = rand;
        shieldBash.FollowUpState = rand;

        return new MonsterMoveStateMachine(
            [slash, twinSlash, roll, shieldBash, rand],
            slash);
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature != Creature)
        {
            return;
        }

        var combatState = Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        var allies = combatState.Enemies
            .Where(c => c != Creature && c.IsAlive && c.Monster is WATCHER)
            .ToList();

        if (allies.Count == 0)
        {
            return;
        }

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            allies,
            RageStrength,
            Creature,
            null,
            false);
    }

    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SlashDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task TwinSlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(TwinDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task RollMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(RollDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await CreatureCmd.GainBlock(Creature, RollBlock, ValueProp.Move, null);
    }

    private async Task ShieldBashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(BashDamage)
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
