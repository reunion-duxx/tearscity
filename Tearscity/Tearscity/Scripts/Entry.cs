using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop;

namespace Tearscity.Scripts;

// 必须要加的属性，用于注册Mod。字符串和初始化函数命名一致。
[ModInitializer(nameof(Init))]
public class Entry
{
    // mod id，必须与 Tearscity.json 里的 "id" 完全一致
    public const string ModId = "Tearscity";

    /// <summary>替换第几幕（0 起）：0 = 第一幕。</summary>
    public const int ActSlot = 0;

    /// <summary>
    /// 打开"完整控制台"：游戏内按 ` （反引号/波浪键）呼出，才能用 <c>fight</c> / <c>kill</c> / <c>god</c> 这类 debug 命令。
    ///
    /// <para>
    /// 游戏自带的 <c>fight &lt;遭遇ID&gt;</c> 是最精确的"指定打哪一场"手段。
    /// 注意：控制台在 mod 载入<b>之前</b>就已经建好（<c>NGame.GameStartup</c> 里 <c>NDevConsole.Create()</c> 早于 mod 检测），
    /// 所以这个开关对<b>下一次启动</b>才生效 —— 部署后重启一次游戏再按 ` 。
    /// </para>
    ///
    /// <para>发布正式版前建议改回 false（它同时会关掉统计/崩溃上报）。</para>
    /// </summary>
    private static readonly bool EnableFullConsole = true;

    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    // 初始化函数
    public static void Init()
    {
        var assembly = typeof(Entry).Assembly;

        // 打patch（即修改游戏代码的功能）用
        // 传入参数随意，只要不和其他人撞车即可
        var harmony = new Harmony("sts2.duxx.tearscity");
        PatchAllSafely(harmony, assembly);

        // 使得tscn可以加载自定义脚本
        ScriptManagerBridge.LookupScriptsInAssembly(assembly);

        // RitsuLib：注册 Godot 脚本 + 扫描本程序集里的内容
        // （扫描会把 [RegisterAct] / [RegisterActEncounter] / [RegisterMonster] 之类的自动注册一并处理，
        //   所以 encounter.cs 里不用再手动 RegisterActEncounter）
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        EnableDevConsoleIfRequested();

        var content = ModContentRegistry.For(ModId);

        // 注册新阶段（泪水之城）
        // 注意：本文件在 namespace Tearscity.Scripts 下，所以 Acts.Tearscity 解析到
        // Tearscity.Scripts.Acts.Tearscity；不要写成裸的 Tearscity（会被解析成命名空间 Tearscity）
        content.RegisterAct(typeof(Acts.Tearscity));

        // 强制在进入第 ActSlot+1 幕时使用本阶段（priority 越大越优先）
        // 例外：如果控制台/别处已经显式把这个槽位换成了我们另一幕（比如用 act 指令换幕测试），
        // 就不要抢回来，否则表现成「换了幕还是第一幕」。
        content.RegisterActEnterForce<Acts.Tearscity>(ActSlot, 100,
            ctx => ctx.EnteringActIndex == ActSlot && !IsActAt(ctx, ActSlot, typeof(Acts.Fungalwastes)));

        // 第二幕：真菌荒地（音乐/事件/Boss 沿用原版第二幕）
        content.RegisterAct(typeof(Acts.Fungalwastes));
        content.RegisterActEnterForce<Acts.Fungalwastes>(1, 100,
            ctx => ctx.EnteringActIndex == 1 && !IsActAt(ctx, 1, typeof(Acts.Tearscity)));

        // 第三幕：感染十字路口（音乐/Boss 沿用原版第三幕，事件自建）
        content.RegisterAct(typeof(Acts.Infectedcrossroads));
        content.RegisterActEnterForce<Acts.Infectedcrossroads>(2, 100,
            ctx => ctx.EnteringActIndex == 2
                   && !IsActAt(ctx, 2, typeof(Acts.Tearscity))
                   && !IsActAt(ctx, 2, typeof(Acts.Fungalwastes)));

        Log.Info("Tearscity initialized! (replacing act " + (ActSlot + 1) + " + act 2 + act 3)");
    }

    private static bool IsActAt(STS2RitsuLib.Content.ActEnterResolveContext ctx, int slot, Type actType)
    {
        try
        {
            var acts = ctx.RunState?.Acts;
            if (acts == null || acts.Count <= slot || slot < 0)
            {
                return false;
            }

            var current = acts[slot];
            return current != null && actType.IsInstanceOfType(current);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 按补丁类逐个打补丁：某个补丁的目标方法在当前游戏版本里不存在时只跳过它，
    /// 不会像 PatchAll 那样整批失败、把模组初始化一起拖崩。
    /// </summary>
    private static void PatchAllSafely(Harmony harmony, System.Reflection.Assembly assembly)
    {
        var patched = 0;
        var skipped = new System.Collections.Generic.List<string>();

        foreach (var type in assembly.GetTypes())
        {
            // 注意：C# 的 static class 在 IL 里是 abstract + sealed，不能因为 IsAbstract 就跳过，
            // 否则所有 [HarmonyPatch] 类（都是 static）都会被漏掉。
            var isStaticClass = type.IsAbstract && type.IsSealed;
            if ((type.IsAbstract && !isStaticClass) || type.IsInterface || type.ContainsGenericParameters)
            {
                continue;
            }

            var attributes = type.GetCustomAttributes(typeof(HarmonyLib.HarmonyPatch), true);
            if (attributes == null || attributes.Length == 0)
            {
                continue;
            }

            try
            {
                harmony.CreateClassProcessor(type).Patch();
                patched++;
            }
            catch (Exception e)
            {
                skipped.Add(type.Name);
                Log.Warn($"[Tearscity] 跳过补丁 {type.Name}（当前游戏版本没有目标方法）：{e.Message}", 2);
            }
        }

        Log.Info($"[Tearscity] 补丁：成功 {patched} 个，跳过 {skipped.Count} 个"
                 + (skipped.Count > 0 ? " -> " + string.Join(", ", skipped) : string.Empty));
    }

    /// <summary>把游戏设置里的 "FullConsole" 打开（持久化到 settings.save，下次启动生效）。</summary>
    private static void EnableDevConsoleIfRequested()
    {
        if (!EnableFullConsole)
        {
            return;
        }

        try
        {
            var saveManager = SaveManager.Instance;
            var settings = saveManager?.SettingsSave;
            if (settings == null || settings.FullConsole)
            {
                return;
            }

            settings.FullConsole = true;
            saveManager!.SaveSettings();
            Log.Info("[Tearscity] 已开启完整控制台：下次启动后按 ` 呼出，可用 fight <遭遇ID> 直接跳战斗");
        }
        catch (Exception e)
        {
            Log.Warn("[Tearscity] 开启完整控制台失败：" + e.Message, 2);
        }
    }
}

