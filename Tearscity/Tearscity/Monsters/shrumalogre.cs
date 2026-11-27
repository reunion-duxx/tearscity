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

namespace shrumalogre.Scripts;

[RegisterMonster]
public class SHRUMAL_OGRE : ModMonsterTemplate
{
    private const int SpawnCount = 3;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 212, 207);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 224, 220);

    private int ThrashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 23, 22);

    private int EmpowerBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

    private int EmpowerStrength => 3;

    private int SpitDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 11);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_shrumalogre.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var thrash = new MoveState(
            "THRASH",
            ThrashMove,
            new SingleAttackIntent(ThrashDamage));

        var empower = new MoveState(
            "EMPOWER",
            EmpowerMove,
            new DefendIntent(),
            new BuffIntent());

        var spit = new MoveState(
            "SPIT",
            SpitMove,
            new MultiAttackIntent(SpitDamage, 2));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(thrash, MoveRepeatType.CanRepeatForever, 3f);
        rand.AddBranch(spit, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(empower, MoveRepeatType.CannotRepeat, 1f);

        thrash.FollowUpState = rand;
        empower.FollowUpState = rand;
        spit.FollowUpState = rand;

        return new MonsterMoveStateMachine([thrash, empower, spit, rand], thrash);
    }

    private async Task ThrashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(ThrashDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task EmpowerMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, EmpowerBlock, ValueProp.Move, null);

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            EmpowerStrength,
            Creature,
            null,
            false);
    }

    private async Task SpitMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SpitDamage)
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
