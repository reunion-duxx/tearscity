using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
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

namespace gluttonous.Scripts;

[RegisterMonster]
public class GLUTTONOUS : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 60, 55);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 68, 62);

    private int SlapDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

    private int SlapHeal => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    private int GorgeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    private int GorgeHeal => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

    private int GorgeBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 7);

    private int BellyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 15);

    private int BellyHeal => 2;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_gluttonoushusk.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var slap = new MoveState(
            "SLAP",
            SlapMove,
            new SingleAttackIntent(SlapDamage));

        var gorge = new MoveState(
            "GORGE",
            GorgeMove,
            new SingleAttackIntent(GorgeDamage),
            new DefendIntent());

        var belly = new MoveState(
            "BELLY_FLOP",
            BellyFlopMove,
            new SingleAttackIntent(BellyDamage));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(slap, MoveRepeatType.CannotRepeat, 3f);
        rand.AddBranch(gorge, MoveRepeatType.CannotRepeat, 1f);
        rand.AddBranch(belly, MoveRepeatType.CannotRepeat, 1f);

        slap.FollowUpState = rand;
        gorge.FollowUpState = rand;
        belly.FollowUpState = rand;

        return new MonsterMoveStateMachine([slap, gorge, belly, rand], slap);
    }

    private async Task SlapMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SlapDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await HealSelf(SlapHeal);
    }

    private async Task GorgeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(GorgeDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await CreatureCmd.GainBlock(Creature, GorgeBlock, ValueProp.Move, null);
        await HealSelf(GorgeHeal);
    }

    private async Task BellyFlopMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(BellyDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await HealSelf(BellyHeal);
    }

    private async Task HealSelf(int amount)
    {
        if (!Creature.IsAlive || Creature.CurrentHp >= Creature.MaxHp)
        {
            return;
        }

        await CreatureCmd.Heal(Creature, amount, true);
    }
}
