using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace heavy.Scripts;

[RegisterMonster]
public class HEAVY : ModMonsterTemplate
{
    private int _attackBonus;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 87, 82);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 98, 91);

    private int StrengthGain => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    private int BaseDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 11);

    private int DamageStep => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_heavy.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var firstPowerUp = new MoveState(
            "POWER_UP",
            PowerUpMove,
            new BuffIntent());

        var secondPowerUp = new MoveState(
            "POWER_UP_2",
            PowerUpMove,
            new BuffIntent());

        var attack = new MoveState(
            "ATTACK",
            AttackMove,
            new SingleAttackIntent(() => (decimal)(BaseDamage + _attackBonus)));

        firstPowerUp.FollowUpState = secondPowerUp;
        secondPowerUp.FollowUpState = attack;
        attack.FollowUpState = attack;

        return new MonsterMoveStateMachine([firstPowerUp, secondPowerUp, attack], firstPowerUp);
    }

    private async Task PowerUpMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            StrengthGain,
            Creature,
            null,
            false);
    }

    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        var damage = BaseDamage + _attackBonus;

        await DamageCmd
            .Attack(damage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        _attackBonus += DamageStep;
    }
}
