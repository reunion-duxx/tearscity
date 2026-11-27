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

namespace soulwarrior.Scripts;

[RegisterMonster]
public class SOUL_WARRIOR : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 117, 107);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 127, 117);

    private int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 12);

    private int DownSlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

    private int BulwarkBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 14);

    private int BulwarkStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    private int SoulBallDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://scenes/creature_visuals/tearscity_monster_soulwarrior.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals() => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var slash = new MoveState(
            "SLASH",
            SlashMove,
            new SingleAttackIntent(SlashDamage));

        var downSlash = new MoveState(
            "DOWN_SLASH",
            DownSlashMove,
            new MultiAttackIntent(DownSlashDamage, 2));

        var bulwark = new MoveState(
            "BULWARK",
            BulwarkMove,
            new DefendIntent(),
            new BuffIntent());

        var soulBall = new MoveState(
            "SOUL_BALL",
            SoulBallMove,
            new MultiAttackIntent(SoulBallDamage, 2),
            new DebuffIntent(false));

        var rand = new RandomBranchState("RAND");
        rand.AddBranch(slash, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(downSlash, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(bulwark, MoveRepeatType.CannotRepeat, 2f);
        rand.AddBranch(soulBall, MoveRepeatType.CannotRepeat, 1f);

        slash.FollowUpState = rand;
        downSlash.FollowUpState = rand;
        bulwark.FollowUpState = rand;
        soulBall.FollowUpState = rand;

        return new MonsterMoveStateMachine([slash, downSlash, bulwark, soulBall, rand], slash);
    }

    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SlashDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task DownSlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(DownSlashDamage)
            .WithHitCount(2)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task BulwarkMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, BulwarkBlock, ValueProp.Move, null);

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            BulwarkStrength,
            Creature,
            null,
            false);
    }

    private async Task SoulBallMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd
            .Attack(SoulBallDamage)
            .WithHitCount(2)
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
