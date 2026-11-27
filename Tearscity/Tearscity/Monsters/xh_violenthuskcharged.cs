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

namespace violenthuskcharged.Scripts;

[RegisterMonster]
public class VIOLENT_HUSK_CHARGED : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 320, 300);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 336, 316);

    private int RushDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 36, 34);

    private int RampageDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 12);

    private int SelfDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 6);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_violenthuskcharged.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var rush = new MoveState(
            "CHARGED_RUSH",
            RushMove,
            new SingleAttackIntent(RushDamage));

        var rampage = new MoveState(
            "RAMPAGE",
            RampageMove,
            new MultiAttackIntent(RampageDamage, 3));

        var charge = new MoveState(
            "CHARGE_UP",
            ChargeUpMove,
            new BuffIntent());

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(rush, MoveRepeatType.CanRepeatForever, 3f);
        rand.AddBranch(rampage, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(charge, MoveRepeatType.CannotRepeat, 1f);

        rush.FollowUpState = rand;
        rampage.FollowUpState = rand;
        charge.FollowUpState = rand;

        return new MonsterMoveStateMachine([rush, rampage, charge, rand], rush);
    }

    private async Task RushMove(IReadOnlyList<Creature> targets)
    {
        if (Creature.CurrentHp > SelfDamage + 1)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                Creature,
                SelfDamage,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null,
                null);
        }

        await DamageCmd
            .Attack(RushDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task RampageMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(RampageDamage)
            .WithHitCount(3)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task ChargeUpMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            2,
            Creature,
            null,
            false);
    }
}
