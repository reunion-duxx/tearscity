using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Unlocks;

namespace Tearscity.Scripts.Patches;

/// <summary>
/// 把「原版共享事件」从本幕的事件池里剔掉。
///
/// <para>
/// 光覆写 <see cref="ActModel.AllEvents"/> 是不够的：<c>ActModel.GenerateRooms(rng, unlockState, ...)</c>
/// 拼事件池时是这么写的：
/// </para>
/// <code>
/// AllEvents.Concat(ModelDb.AllSharedEvents).ToList()
/// </code>
/// <para>
/// 而 <c>ModelDb.AllSharedEvents</c> 是<b>写死在游戏里的 19 个原版事件</b>
/// （BrainLeech / CrystalSphere / DollRoom / FakeMerchant / MuseumOfTorture / PotionCourier /
/// RanwidTheElder / RelicTrader …），它们会出现在**每一幕**，跟当前是哪一幕无关。
/// 所以就算本幕的 AllEvents 只有 13 个自建事件，随机事件房里还是有一半概率抽到原版的。
/// </para>
/// <para>
/// 这里在房间生成之后、按「只保留本幕自建事件」过滤一次 <c>RoomSet.events</c>。
/// 只对泪水之城生效，原版三幕的共享事件照旧。
/// </para>
/// </summary>
[HarmonyPatch(typeof(ActModel), "GenerateRooms")]
internal static class TearscityEventPoolPatch
{
    private static readonly FieldInfo? RoomsField =
        typeof(ActModel).GetField("_rooms", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? EventsField =
        typeof(RoomSet).GetField("events", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private static void Postfix(ActModel __instance)
    {
        if (__instance is not Acts.Tearscity)
        {
            return;
        }

        try
        {
            if (RoomsField?.GetValue(__instance) is not RoomSet rooms)
            {
                return;
            }

            if (EventsField?.GetValue(rooms) is not List<EventModel> events)
            {
                return;
            }

            var allowed = new HashSet<ModelId>(__instance.AllEvents.Select(e => e.Id));
            var before = events.Count;
            events.RemoveAll(e => !allowed.Contains(e.Id));

            Log.Info($"[Tearscity] 事件池：保留 {events.Count} 个自建事件，剔除原版共享事件 {before - events.Count} 个");
        }
        catch (Exception e)
        {
            Log.Warn($"[Tearscity] 事件池过滤失败：{e}", 2);
        }
    }
}
