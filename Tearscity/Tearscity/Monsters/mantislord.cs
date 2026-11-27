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

namespace mantislord.Scripts;

[RegisterMonster]
public class MANTIS_LORD : ModMonsterTemplate
{
    private bool _summoningTwins;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 200, 190);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 220, 210);

    private int ScytheDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 16);

    private int DashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 25, 24);

    private int DiveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 27, 26);

    private int BowBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

    private int BowStrength => 2;

    private int FrailAmount => 1;

    private int VulnerableAmount => 1;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_mantislord.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var scythe = new MoveState(
            "WIND_SCYTHE",
            ScytheMove,
            new SingleAttackIntent(ScytheDamage),
            new DebuffIntent(false));

        var dash = new MoveState(
            "DASH",
            DashMove,
            new SingleAttackIntent(DashDamage));

        var dive = new MoveState(
            "DIVE",
            DiveMove,
            new SingleAttackIntent(DiveDamage),
            new DebuffIntent(false));

        var bow = new MoveState(
            "BOW",
            BowMove,
            new DefendIntent(),
            new BuffIntent());

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(dash, MoveRepeatType.CanRepeatForever, 3f);
        rand.AddBranch(scythe, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(dive, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(bow, MoveRepeatType.UseOnlyOnce, 1f);

        dash.FollowUpState = rand;
        scythe.FollowUpState = rand;
        dive.FollowUpState = rand;
        bow.FollowUpState = rand;

        return new MonsterMoveStateMachine([dash, scythe, dive, bow, rand], rand);
    }

    public override bool ShouldStopCombatFromEnding() => _summoningTwins;

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

        _summoningTwins = true;
        var spawned = 0;
        try
        {
            for (var i = 0; i < 2; i++)
            {
                var slot = encounter.GetNextSlot(combatState);
                if (string.IsNullOrEmpty(slot))
                {
                    break;
                }

                await CreatureCmd.Add<mantislordtwin.Scripts.MANTIS_LORD_TWIN>(combatState, slot);
                spawned++;
            }
        }
        finally
        {
            _summoningTwins = spawned > 0;
        }
    }

    private async Task ScytheMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(ScytheDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await PowerCmd.Apply<FrailPower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            FrailAmount,
            Creature,
            null,
            false);
    }

    private async Task DashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(DashDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task DiveMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(DiveDamage)
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

    private async Task BowMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, BowBlock, ValueProp.Move, null);

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            BowStrength,
            Creature,
            null,
            false);
    }
}
