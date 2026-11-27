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

namespace aspidmother.Scripts;

[RegisterMonster]
public class ASPID_MOTHER : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 175, 160);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 185, 170);

    private int SpitDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 14);

    private int LungeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_aspidmother.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    private const int MaxHatchlings = 2;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var spit = new MoveState(
            "SPIT",
            SpitMove,
            new SingleAttackIntent(SpitDamage));

        var brood = new MoveState(
            "BROOD",
            BroodMove,
            new SummonIntent());

        var lunge = new MoveState(
            "LUNGE",
            LungeMove,
            new SingleAttackIntent(LungeDamage));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(spit, MoveRepeatType.CanRepeatForever, 2f);
        rand.AddBranch(lunge, MoveRepeatType.CanRepeatForever, 2f);
        rand.AddBranch(brood, MoveRepeatType.CannotRepeat, 2f);

        spit.FollowUpState = rand;
        lunge.FollowUpState = rand;
        brood.FollowUpState = rand;

        return new MonsterMoveStateMachine([spit, brood, lunge, rand], spit);
    }

    private async Task SpitMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SpitDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task LungeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(LungeDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task BroodMove(IReadOnlyList<Creature> targets)
    {
        var combatState = CombatState;
        var encounter = combatState?.Encounter;
        if (combatState == null || encounter == null || !combatState.IsLiveCombat())
        {
            return;
        }

        var hatched = combatState.Enemies.Count(c => c.IsAlive && c.Monster is aspidhatchling.Scripts.ASPID_HATCHLING);
        if (hatched >= MaxHatchlings)
        {
            return;
        }

        var slot = encounter.GetNextSlot(combatState);
        if (string.IsNullOrEmpty(slot))
        {
            return;
        }

        await CreatureCmd.Add<aspidhatchling.Scripts.ASPID_HATCHLING>(combatState, slot);
    }
}
