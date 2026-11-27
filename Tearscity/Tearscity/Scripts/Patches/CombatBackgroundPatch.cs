using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace Tearscity.Scripts.Patches;

/// <summary>
/// 背景场景的接管补丁。
///
/// 原版流程：<c>NCombatBackground.Create(BackgroundAssets)</c> 会加载
/// <c>res://scenes/backgrounds/&lt;id&gt;/&lt;id&gt;_background.tscn</c>，并把根节点
/// <b>强转成 NCombatBackground</b>。而 mod 的 pck 里引用不了游戏自己的 C# 脚本：
/// 导出时 Godot 必须先 load 那个 .cs 才能建立引用，load 不到就把 script 丢掉
/// （导出日志：Cannot set object script），运行时根节点就退化成普通 Control → 强转失败 → 进战斗卡死。
///
/// 所以这里接管 Create：对本 mod 的幕，用 C# 现场拼出和原版一样的节点树
/// （NCombatBackground 根 + Layer_00.. + Foreground 铺满全屏的容器），再把 layers 目录里
/// 的分层场景挨个挂进去。其余幕一律放行给原版。
/// </summary>
[HarmonyPatch(typeof(NCombatBackground), nameof(NCombatBackground.Create))]
internal static class CombatBackgroundPatch
{
    /// <summary>
    /// RitsuLib 会给 mod 模型加前缀：类 <c>Acts.Tearscity</c> 的 Entry 是
    /// <c>TEARSCITY_ACT_TEARSCITY</c>，FilePathIdentifier 小写成 <c>tearscity_act_tearscity</c>，
    /// 资源路径就是按它拼的，所以直接拿它当识别标记。
    /// </summary>
    private const string ActFilePath = "tearscity_act_tearscity";

    private const string FungalActFilePath = "tearscity_act_fungalwastes";

    private const string CrossroadsActFilePath = "tearscity_act_infectedcrossroads";

    private static bool IsOurAct(string? scenePath)
    {
        return scenePath != null
               && (scenePath.Contains(ActFilePath) || scenePath.Contains(FungalActFilePath) || scenePath.Contains(CrossroadsActFilePath));
    }

    private static bool Prefix(BackgroundAssets bg, ref NCombatBackground __result)
    {
        var scenePath = bg?.BackgroundScenePath;
        if (!IsOurAct(scenePath))
        {
            return true;
        }

        __result = Build(bg!);
        return false;
    }

    private static NCombatBackground Build(BackgroundAssets bg)
    {
        var root = new NCombatBackground { Name = "TearscityBackground" };
        Fill(root);
        root.Size = new Vector2(1920f, 1080f);

        // 层名和原版一致：第 i 层挂在 "Layer_" + i.ToString("D2") 上，前景挂在 "Foreground" 上。
        var containers = new List<Control>();
        for (var i = 0; i < bg.BgLayers.Count; i++)
        {
            containers.Add(AddLayer(root, "Layer_" + i.ToString("D2"), bg.BgLayers[i]));
        }

        if (!string.IsNullOrEmpty(bg.FgLayer))
        {
            containers.Add(AddLayer(root, "Foreground", bg.FgLayer));
        }

        root.TreeEntered += () =>
        {
            var viewport = root.GetViewport();
            if (viewport == null)
            {
                return;
            }

            var viewportSize = viewport.GetVisibleRect().Size;
            if (viewportSize.X < 2f || viewportSize.Y < 2f)
            {
                return;
            }

            ApplyLayout(root, containers, viewportSize);

            var framesLeft = 6;
            void Reapply()
            {
                framesLeft--;
                ApplyLayout(root, containers, viewportSize);

                if (framesLeft > 0)
                {
                    return;
                }

                root.GetTree().ProcessFrame -= Reapply;
            }

            root.GetTree().ProcessFrame += Reapply;
        };

        return root;
    }

    private static Control AddLayer(NCombatBackground root, string nodeName, string layerPath)
    {
        var container = new Control { Name = nodeName };
        Fill(container);
        root.AddChild(container);

        // 层场景在进房前已被 PreloadManager 预加载，这里直接从资源缓存取
        var scene = GD.Load<PackedScene>(layerPath);
        if (scene == null)
        {
            Log.Warn($"[Tearscity] background layer missing: {layerPath}", 2);
            return container;
        }

        var layer = scene.Instantiate();
        if (layer is CanvasItem canvas)
        {
            canvas.Visible = true;
        }

        if (layer is Control control)
        {
            control.CustomMinimumSize = new Vector2(1920f, 1080f);
        }

        container.AddChild(layer);
        return container;
    }

    /// <summary>铺满父容器，保证分层贴图撑满整个战斗画面。</summary>
    private static void Fill(Control control)
    {
        control.AnchorLeft = 0f;
        control.AnchorTop = 0f;
        control.AnchorRight = 1f;
        control.AnchorBottom = 1f;
        control.OffsetLeft = 0f;
        control.OffsetTop = 0f;
        control.OffsetRight = 0f;
        control.OffsetBottom = 0f;
        control.GrowHorizontal = Control.GrowDirection.End;
        control.GrowVertical = Control.GrowDirection.End;
        control.CustomMinimumSize = new Vector2(1920f, 1080f);
    }

    private static void ApplyLayout(NCombatBackground root, List<Control> containers, Vector2 size)
    {
        var layerScale = new Vector2(size.X / 1920f, size.Y / 1080f);

        root.Position = Vector2.Zero;
        root.PivotOffset = Vector2.Zero;
        root.Scale = Vector2.One;
        root.Size = size;

        foreach (var container in containers)
        {
            container.Position = Vector2.Zero;
            container.PivotOffset = Vector2.Zero;
            container.Scale = Vector2.One;
            container.Size = size;

            var origin = container.GetGlobalTransform().AffineInverse() * Vector2.Zero;

            foreach (var child in container.GetChildren())
            {
                if (child is Node2D node2D)
                {
                    node2D.Position = origin;
                    node2D.Scale = layerScale;
                }
                else if (child is Control control)
                {
                    control.Position = origin;
                    control.PivotOffset = Vector2.Zero;
                    control.Scale = layerScale;
                    control.Size = new Vector2(1920f, 1080f);
                }
            }
        }
    }
}
