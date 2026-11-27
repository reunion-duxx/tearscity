using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace shrumalwarrior.Scripts;

[RegisterMonster]
public class SHRUMAL_WARRIOR : ModMonsterTemplate
{
    private const int SpawnCount = 2;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 102, 98);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 115, 110);

    private int ChargeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

    private int GuardBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

    private int GuardStrength => 2;

    private int StompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_shrumalwarrior.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var charge = new MoveState(
            "CHARGE",
            ChargeMove,
            new SingleAttackIntent(ChargeDamage));

        var guard = new MoveState(
            "GUARD",
            GuardMove,
            new DefendIntent(),
            new BuffIntent());

        var stomp = new MoveState(
            "STOMP",
            StompMove,
            new MultiAttackIntent(StompDamage, 2));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(charge, MoveRepeatType.CanRepeatForever, 2f);
        rand.AddBranch(stomp, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(guard, MoveRepeatType.CannotRepeat, 1f);

        charge.FollowUpState = rand;
        guard.FollowUpState = rand;
        stomp.FollowUpState = rand;

        return new MonsterMoveStateMachine([charge, guard, stomp, rand], charge);
    }

    private async Task ChargeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(ChargeDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
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

    private async Task StompMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(StompDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature != Creature)
        {
            return;
        }

        var combatState = CombatState;
        var encounter = combatState?.Encounter;
        if (combatState == null || encounter == null || !combatState.IsLiveCombat())
        {
            return;
        }

        for (var i = 0; i < SpawnCount; i++)
        {
            var slot = encounter.GetNextSlot(combatState);
            if (string.IsNullOrEmpty(slot))
            {
                return;
            }

            await CreatureCmd.Add<shrumeling.Scripts.SHRUMELING>(combatState, slot);
        }
    }

}
