using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
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

namespace gorgeoushusk.Scripts;

[RegisterMonster]
public class GORGEOUS : ModMonsterTemplate
{
    private const decimal GoldSteal = 10m;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 104, 91);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 117, 101);

    private int SlapDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

    private int AdornBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 19, 17);

    private int AdornStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    private int SparkleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_gorgeous.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var slap = new MoveState(
            "GILDED_SLAP",
            SlapMove,
            new SingleAttackIntent(SlapDamage));

        var adorn = new MoveState(
            "ADORN",
            AdornMove,
            new DefendIntent(),
            new BuffIntent());

        var sparkle = new MoveState(
            "SPARKLE",
            SparkleMove,
            new MultiAttackIntent(SparkleDamage, 2),
            new DebuffIntent(false));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(slap, MoveRepeatType.CannotRepeat, 3f);
        rand.AddBranch(sparkle, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(adorn, MoveRepeatType.CannotRepeat, 1f);

        slap.FollowUpState = rand;
        sparkle.FollowUpState = rand;
        adorn.FollowUpState = rand;

        return new MonsterMoveStateMachine([slap, sparkle, adorn, rand], slap);
    }

    private async Task SlapMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SlapDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await StealGold(targets, GoldSteal);
    }

    private async Task AdornMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, AdornBlock, ValueProp.Move, null);

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            AdornStrength,
            Creature,
            null,
            false);
    }

    private async Task SparkleMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SparkleDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await PowerCmd.Apply<WeakPower>(
            new ThrowingPlayerChoiceContext(),
            targets,
            WeakAmount,
            Creature,
            null,
            false);
    }

    private static async Task StealGold(IReadOnlyList<Creature> targets, decimal amount)
    {
        var player = targets.FirstOrDefault()?.Player;
        if (player == null || player.Gold < amount)
        {
            return;
        }

        await PlayerCmd.LoseGold(amount, player, GoldLossType.Stolen);
    }
}
