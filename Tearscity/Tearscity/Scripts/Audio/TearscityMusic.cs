using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace Tearscity.Scripts.Audio;

/// <summary>当前音乐情绪：地图/事件用 Calm，战斗用 Combat。</summary>
public enum MusicMood
{
    Calm,
    Combat
}

/// <summary>
/// 本 mod 的背景音乐播放层。
///
/// 为什么不用原版的 FMOD bank：
/// 原版一幕的音乐是打包成 FMOD bank 的（<c>act1_a1.bank</c>），自己做音乐要另外装 FMOD Studio、
/// 建工程、编 event、导出 bank，对没做过音乐的人来说门槛太高，而且改一次音乐就要重导一次 bank。
/// 所以这里走社区里已经验证可行的另一条路：用 Godot 自带的 AudioStreamPlayer 直接播 ogg/mp3/wav，
/// 再把原版那一套 FMOD 音乐绕过去（见 <see cref="Tearscity.Scripts.Patches.TearscityMusicPatch"/>）。
///
/// 音频文件放在 mod 目录的 <c>assets/music/</c> 下，按名字自动找 .ogg / .mp3 / .wav：
///     tearscity_calm      地图、休息处、商店、事件
///     tearscity_combat    普通战斗、精英、Boss
///     tearscity_ambience  常驻环境声（雨声）
/// 换音乐 = 换文件，不用重新编译，也不用重导 pck。
/// </summary>
public static class TearscityMusic
{
    /// <summary>总开关：想临时回到原版音乐，把它改成 false 即可。</summary>
    public static readonly bool EnableCustomMusic = true;

    private const string MusicFolder = "assets/music";
    private const string CalmName = "tearscity_calm";
    private const string CombatName = "tearscity_combat";
    private const string AmbienceName = "tearscity_ambience";

    private const float FadeSeconds = 1.6f;
    private const float CalmDb = -6f;
    private const float CombatDb = -4f;
    private const float AmbienceDb = -12f;
    private const float SilentDb = -60f;

    private static Node? _host;
    private static AudioStreamPlayer? _calm;
    private static AudioStreamPlayer? _combat;
    private static AudioStreamPlayer? _ambience;

    private static Tween? _calmTween;
    private static Tween? _combatTween;
    private static Tween? _ambienceTween;

    private static bool _loaded;
    private static bool _active;
    private static MusicMood _mood = MusicMood.Calm;

    /// <summary>游戏设置里的音乐音量（0..1），由 <see cref="Tearscity.Scripts.Patches.TearscityMusicPatch"/> 同步过来。</summary>
    private static float _gameVolume = 1f;

    private static float _savedGameVolume = 1f;

    private static bool _mutingVanilla;

    public static bool IsActive => _active;

    // ---------------------------------------------------------------- 生命周期

    /// <summary>进入本 mod 的幕：停掉原版音乐、开始播自己的 BGM 与环境声。</summary>
    public static void EnterAct()
    {
        if (!EnableCustomMusic)
        {
            return;
        }

        if (_active)
        {
            return;
        }

        EnsureHost();
        if (_host == null)
        {
            return;
        }

        // 原版的 FMOD 音乐（主菜单/上一幕）可能还在响，先掐掉。
        // 注意顺序：这一步必须在 _active = true 之前 —— NAudioManager.StopMusic 会进
        // TearscityStopMusicPatch，那里会调 LeaveAct()，若此时已经 active 就会把自己刚开的音乐关掉。
        TryStopVanillaMusic();
        MuteVanillaMusic();

        _active = true;
        _mood = MusicMood.Calm;

        PlayAmbience();
        CrossFade(_calm, CalmDb, ref _calmTween);
        StopSoftly(_combat, ref _combatTween);

        Log.Info($"[Tearscity] music: enter act, calm={_calm?.Stream?.GetLength() ?? 0f:F1}s " +
                 $"combat={_combat?.Stream?.GetLength() ?? 0f:F1}s");
    }

    /// <summary>离开本 mod 的幕 / 回主菜单 / 结束一局：淡出全部音乐。</summary>
    public static void LeaveAct()
    {
        if (!_active)
        {
            return;
        }

        _active = false;
        CrossFade(_calm, SilentDb, ref _calmTween, stopWhenDone: true);
        CrossFade(_combat, SilentDb, ref _combatTween, stopWhenDone: true);
        CrossFade(_ambience, SilentDb, ref _ambienceTween, stopWhenDone: true);
        RestoreVanillaMusic();
    }

    /// <summary>房间类型变化时切歌（战斗/精英/Boss 换战斗曲，其余换平静曲）。</summary>
    public static void SetMood(MusicMood mood)
    {
        if (!_active || _mood == mood)
        {
            return;
        }

        _mood = mood;

        // 兜底：万一原版 FMOD 音乐还在响（例如开局的 bank 在换幕之前就加载过），先掐掉再切。
        TryStopVanillaMusic();

        if (mood == MusicMood.Combat)
        {
            CrossFade(_combat, CombatDb, ref _combatTween);
            CrossFade(_calm, SilentDb, ref _calmTween, stopWhenDone: true);
        }
        else
        {
            CrossFade(_calm, CalmDb, ref _calmTween);
            CrossFade(_combat, SilentDb, ref _combatTween, stopWhenDone: true);
        }
    }

    /// <summary>环境声丢失时（例如加载存档后补播）重新拉起。</summary>
    public static void EnsureAmbience()
    {
        if (_active)
        {
            PlayAmbience();
        }
    }

    /// <summary>同步游戏设置里的音乐音量。</summary>
    public static void SetGameVolume(float volume)
    {
        if (_mutingVanilla)
        {
            return;
        }

        _gameVolume = Mathf.Clamp(volume, 0f, 1f);
        _savedGameVolume = _gameVolume;
        ApplyVolumes();
    }

    // ---------------------------------------------------------------- 内部实现

    private static string MusicDirectory
    {
        get
        {
            var dllDir = Path.GetDirectoryName(typeof(TearscityMusic).Assembly.Location);
            return Path.Combine(string.IsNullOrEmpty(dllDir) ? "." : dllDir, MusicFolder);
        }
    }

    private static float VolumeOffsetDb => Mathf.LinearToDb(Mathf.Max(_gameVolume, 0.0001f));

    private static void ApplyVolumes()
    {
        if (!_active)
        {
            return;
        }

        if (_calm is { Playing: true })
        {
            _calm.VolumeDb = CalmDb + VolumeOffsetDb;
        }

        if (_combat is { Playing: true })
        {
            _combat.VolumeDb = CombatDb + VolumeOffsetDb;
        }

        if (_ambience is { Playing: true })
        {
            _ambience.VolumeDb = AmbienceDb + VolumeOffsetDb;
        }
    }

    private static void EnsureHost()
    {
        if (_host != null && GodotObject.IsInstanceValid(_host))
        {
            return;
        }

        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            Log.Warn("[Tearscity] music: main loop is not a SceneTree", 2);
            return;
        }

        try
        {
            _host = new Node { Name = "TearscityMusic" };
            tree.Root.AddChild(_host);

            _calm = MakePlayer(_host, "Calm");
            _combat = MakePlayer(_host, "Combat");
            _ambience = MakePlayer(_host, "Ambience");

            _calm.Stream = LoadStream(CalmName);
            _combat.Stream = LoadStream(CombatName);
            _ambience.Stream = LoadStream(AmbienceName);
            _loaded = true;

            Log.Info($"[Tearscity] music ready: calm={_calm.Stream != null} combat={_combat.Stream != null} " +
                     $"ambience={_ambience.Stream != null} dir={MusicDirectory}");
        }
        catch (Exception e)
        {
            Log.Warn($"[Tearscity] music: failed to build player: {e}", 2);
            _host = null;
        }
    }

    private static AudioStreamPlayer MakePlayer(Node host, string name)
    {
        var player = new AudioStreamPlayer
        {
            Name = name,
            VolumeDb = SilentDb,
            Autoplay = false,
            // 直接用 Master 总线：游戏设置里的音乐音量我们自己同步（见 SetGameVolume），
            // 这样不同 mod 之间不会互相抢总线。
            Bus = "Master",
            ProcessMode = Node.ProcessModeEnum.Always
        };
        host.AddChild(player);
        return player;
    }

    private static void PlayAmbience()
    {
        if (!_loaded || _ambience == null)
        {
            return;
        }

        CrossFade(_ambience, AmbienceDb, ref _ambienceTween);
    }

    private static void TryStopVanillaMusic()
    {
        var manager = NAudioManager.Instance;
        if (manager == null)
        {
            return;
        }

        try
        {
            manager.StopMusic();
        }
        catch (Exception e)
        {
            Log.Warn($"[Tearscity] music: StopMusic failed: {e.Message}", 2);
        }
    }

    /// <summary>
    /// 把原版 FMOD 的「音乐总线」静音。
    ///
    /// 光跳过 UpdateTrack 只能让原版不切歌，但开局时加载过的原版 bank / 事件还可能被别的路径触发
    /// （例如战斗中某段逻辑直接让 FMOD 播它自己的战斗曲）。我们自己播的东西走 Godot 的 Master 总线，
    /// 不经过 FMOD，所以把 FMOD 的音乐总线按到 0 是安全的兜底。
    /// </summary>
    private static void MuteVanillaMusic()
    {
        var manager = NAudioManager.Instance;
        if (manager == null)
        {
            return;
        }

        _savedGameVolume = _gameVolume;
        _mutingVanilla = true;
        try
        {
            manager.SetBgmVol(0f);
            Log.Info("[Tearscity] music: mute vanilla bgm bus");
        }
        catch (Exception e)
        {
            Log.Warn($"[Tearscity] music: SetBgmVol(0) failed: {e.Message}", 2);
        }
        finally
        {
            _mutingVanilla = false;
        }
    }

    /// <summary>离开本幕时把原版音乐总线音量还回去。</summary>
    private static void RestoreVanillaMusic()
    {
        var manager = NAudioManager.Instance;
        if (manager == null)
        {
            return;
        }

        _mutingVanilla = true;
        try
        {
            manager.SetBgmVol(_savedGameVolume);
        }
        catch (Exception e)
        {
            Log.Warn($"[Tearscity] music: SetBgmVol(restore) failed: {e.Message}", 2);
        }
        finally
        {
            _mutingVanilla = false;
        }
    }

    /// <summary>按名字找文件（.ogg 优先，其次 .mp3、.wav），并设成循环。</summary>
    private static AudioStream? LoadStream(string baseName)
    {
        var dir = MusicDirectory;
        foreach (var extension in new[] { ".ogg", ".mp3", ".wav" })
        {
            var path = Path.Combine(dir, baseName + extension);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                AudioStream? stream = extension switch
                {
                    ".ogg" => AudioStreamOggVorbis.LoadFromFile(path),
                    ".mp3" => AudioStreamMP3.LoadFromFile(path),
                    _ => AudioStreamWav.LoadFromFile(path),
                };

                if (stream == null)
                {
                    continue;
                }

                switch (stream)
                {
                    case AudioStreamOggVorbis ogg:
                        ogg.Loop = true;
                        break;
                    case AudioStreamMP3 mp3:
                        mp3.Loop = true;
                        break;
                    case AudioStreamWav wav:
                        wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
                        wav.LoopBegin = 0;
                        wav.LoopEnd = Mathf.Max(1, (int)(wav.GetLength() * wav.MixRate));
                        break;
                }

                return stream;
            }
            catch (Exception e)
            {
                Log.Warn($"[Tearscity] music: cannot load {path}: {e.Message}", 2);
            }
        }

        Log.Warn($"[Tearscity] music: {baseName}.(ogg|mp3|wav) not found in {dir}", 2);
        return null;
    }

    private static void CrossFade(AudioStreamPlayer? player, float targetDb, ref Tween? slot, bool stopWhenDone = false)
    {
        if (player == null || player.Stream == null)
        {
            return;
        }

        slot?.Kill();
        slot = null;

        if (!player.Playing)
        {
            player.VolumeDb = SilentDb;
            player.Play();
        }

        var tween = player.CreateTween();
        tween.TweenProperty(player, "volume_db", targetDb + (targetDb > SilentDb ? VolumeOffsetDb : 0f), FadeSeconds)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);

        if (stopWhenDone)
        {
            tween.Finished += () =>
            {
                if (player.VolumeDb <= SilentDb + 1f)
                {
                    player.Stop();
                }
            };
        }

        slot = tween;
    }

    private static void StopSoftly(AudioStreamPlayer? player, ref Tween? slot)
    {
        if (player == null || !player.Playing)
        {
            return;
        }

        CrossFade(player, SilentDb, ref slot, stopWhenDone: true);
    }
}
