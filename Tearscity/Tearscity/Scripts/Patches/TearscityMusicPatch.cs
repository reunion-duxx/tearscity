using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Tearscity.Scripts.Audio;

namespace Tearscity.Scripts.Patches;

/// <summary>
/// 把原版「一幕一首 FMOD 歌」的音乐流程接管成「本 mod 自己播 ogg」。
///
/// 具体做了四件事：
/// 1. <c>NRunMusicController.UpdateMusic</c>（进幕/读档时调用）→ 判断当前幕是不是泪水之城，
///    是就开自己的音乐，不是就淡出（比如进第二幕时把手交还给原版）；
/// 2. <c>NRunMusicController.UpdateAmbience</c> → 本幕直接跳过原版环境声，用自己的雨声；
/// 3. <c>NRunMusicController.UpdateTrack(RoomType)</c> → 换房间时切「平静 / 战斗」两首；
/// 4. <c>NRunMusicController.StopMusic</c> 和 <c>NAudioManager.PlayMusic</c>（回主菜单）→ 淡出。
///
/// 另外 <c>NAudioManager.SetBgmVol</c> 的补丁只是「抄一份音量出来」，让设置里的音乐滑条对我们自己的
/// 播放器也生效（我们自己播的东西不经过 FMOD 总线，所以不会自动跟着调）。
/// </summary>
internal static class MusicContext
{
    private static readonly FieldInfo? RunStateField =
        typeof(NRunMusicController).GetField("_runState", BindingFlags.Instance | BindingFlags.NonPublic);

    public static IRunState? StateOf(NRunMusicController controller)
    {
        try
        {
            return RunStateField?.GetValue(controller) as IRunState;
        }
        catch
        {
            return null;
        }
    }

    public static bool IsOurAct(NRunMusicController controller)
    {
        try
        {
            return StateOf(controller)?.Act is Acts.Tearscity;
        }
        catch
        {
            return false;
        }
    }
}

[HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.UpdateMusic))]
internal static class TearscityMusicPatch
{
    private static void Postfix(NRunMusicController __instance)
    {
        if (!TearscityMusic.EnableCustomMusic)
        {
            return;
        }

        try
        {
            if (MusicContext.IsOurAct(__instance))
            {
                TearscityMusic.EnterAct();
            }
            else
            {
                TearscityMusic.LeaveAct();
            }
        }
        catch (Exception e)
        {
            Log.Warn($"[Tearscity] music: UpdateMusic patch failed: {e}", 2);
        }
    }
}

[HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.UpdateAmbience))]
internal static class TearscityAmbiencePatch
{
    private static bool Prefix(NRunMusicController __instance)
    {
        if (!TearscityMusic.EnableCustomMusic)
        {
            return true;
        }

        try
        {
            if (!MusicContext.IsOurAct(__instance))
            {
                return true;
            }

            TearscityMusic.EnterAct();
            TearscityMusic.EnsureAmbience();
            return false;
        }
        catch (Exception e)
        {
            Log.Warn($"[Tearscity] music: UpdateAmbience patch failed: {e}", 2);
            return true;
        }
    }
}

internal static class TrackPatchLogic
{
    /// <summary>
    /// 本幕：跳过原版 UpdateTrack（它会切 FMOD 的 track），只切我们自己的 calm / combat。
    ///
    /// 必须按「重载签名」打补丁：NRunMusicController 有三个 UpdateTrack（无参 / RoomType / (string,float)），
    /// 只写方法名会 Ambiguous match，RitsuLib 的补丁守卫会直接跳过整个补丁类
    /// （日志：跳过补丁 TearscityTrackPatch … Ambiguous match）。
    /// </summary>
    public static bool SkipAndApply(NRunMusicController instance, RoomType? hint)
    {
        if (!TearscityMusic.EnableCustomMusic)
        {
            return false;
        }

        try
        {
            if (!MusicContext.IsOurAct(instance))
            {
                return false;
            }

            RoomType? current = null;
            try
            {
                current = MusicContext.StateOf(instance)?.CurrentRoom?.RoomType;
            }
            catch
            {
            }

            var room = hint ?? current;
            var combat = room is RoomType.Monster or RoomType.Elite or RoomType.Boss;

            Log.Info($"[Tearscity] music: UpdateTrack hint={(hint?.ToString() ?? "-")} current={(current?.ToString() ?? "-")} -> {(combat ? "combat" : "calm")}");
            TearscityMusic.SetMood(combat ? MusicMood.Combat : MusicMood.Calm);
            return true;
        }
        catch (Exception e)
        {
            Log.Warn($"[Tearscity] music: UpdateTrack patch failed: {e}", 2);
            return false;
        }
    }
}

[HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.UpdateTrack), new[] { typeof(RoomType) })]
internal static class TearscityTrackPatch
{
    private static bool Prefix(NRunMusicController __instance, RoomType roomType)
    {
        return TrackPatchLogic.SkipAndApply(__instance, roomType);
    }
}

[HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.UpdateTrack), new Type[0])]
internal static class TearscityTrackNoArgPatch
{
    private static bool Prefix(NRunMusicController __instance)
    {
        return TrackPatchLogic.SkipAndApply(__instance, null);
    }
}

[HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.StopMusic))]
internal static class TearscityStopMusicPatch
{
    private static void Prefix()
    {
        if (TearscityMusic.EnableCustomMusic)
        {
            TearscityMusic.LeaveAct();
        }
    }
}

[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.PlayMusic))]
internal static class TearscityMenuMusicPatch
{
    private static void Prefix()
    {
        if (TearscityMusic.EnableCustomMusic)
        {
            TearscityMusic.LeaveAct();
        }
    }
}

[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.SetBgmVol))]
internal static class TearscityVolumePatch
{
    private static void Postfix(float volume)
    {
        if (TearscityMusic.EnableCustomMusic)
        {
            TearscityMusic.SetGameVolume(volume);
        }
    }
}
