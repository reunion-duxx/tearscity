using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;

namespace Tearscity.Scripts.Patches;

public static class TopBarBossIconRefresh
{
    public static void RefreshNow()
    {
        try
        {
            var icon = NRun.Instance?.GlobalUi?.TopBar?.BossIcon;
            icon?.RefreshBossIcon();
        }
        catch (Exception e)
        {
            Log.Warn("[Tearscity] refresh top bar boss icon failed: " + e.Message, 2);
        }
    }

    public static async Task ScheduleAsync()
    {
        try
        {
            Callable.From(RefreshNow).CallDeferred();
            RefreshNow();
            await Cmd.Wait(0.6f, true);
            RefreshNow();
            await Cmd.Wait(2f, true);
            RefreshNow();
        }
        catch (Exception e)
        {
            Log.Warn("[Tearscity] schedule top bar boss icon refresh failed: " + e.Message, 2);
        }
    }
}
