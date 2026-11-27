"""
泪水之城 BGM 生成器（程序化配乐，无版权问题，改参数即可换风格）

产出三首无缝循环：
    tearscity_calm.wav      平静 / 地图 / 休息处（钢琴 + 弦垫 + 贝斯）
    tearscity_combat.wav    战斗 / 精英 / Boss（低音固定音型 + 打击 + 明亮弦垫）
    tearscity_ambience.wav  环境声（雨、滴水、远处城市低鸣）

原理一句话：所有音符按「环形缓冲」写入（时间到了末尾就绕回开头），
混响用 FFT 做「循环卷积」，所以文件首尾天然接得上，不需要淡入淡出。

用法：
    python tools/gen_music.py --out assets/music
    python tools/gen_music.py --out assets/music --only calm

没有任何乐理基础也能改的地方都在下面的「可调参数」里：
调式（NOTE_NAMES / 和弦表）、速度（CHORD_SECONDS）、音符密度、混响长度、雨声大小。
"""

from __future__ import annotations

import argparse
import os
import wave

import numpy as np

SR = 44100  # 采样率

# ============================================================
# 可调参数：想换情绪先动这里
# ============================================================

# 平静曲：A 小调，8 秒一个和弦，4 个和弦循环一次（32 秒），放两遍 = 64 秒
CALM_CHORDS = [
    # (低音 MIDI, 和弦音 MIDI 列表)
    (33, [57, 60, 64, 67, 71]),  # Am9   A C E G B
    (29, [53, 57, 60, 64, 67]),  # Fmaj7 F A C E G
    (38, [50, 53, 57, 60, 64]),  # Dm9   D F A C E
    (40, [52, 56, 59, 62, 65]),  # E7b9  E G# B D F
]

# 战斗曲：D 小调，6 秒一个和弦，4 个和弦 = 24 秒，放两遍 = 48 秒
COMBAT_CHORDS = [
    (38, [50, 53, 57, 62]),  # Dm
    (34, [46, 50, 53, 58]),  # Bb
    (31, [43, 46, 50, 55]),  # Gm
    (33, [45, 49, 52, 57]),  # A
]

CALM_CHORD_SECONDS = 8.0
COMBAT_CHORD_SECONDS = 6.0
CALM_CYCLES = 2
COMBAT_CYCLES = 2

# 混音音量（相对值，随便调）
CALM_PAD_GAIN = 0.17
CALM_PIANO_GAIN = 0.42
CALM_BASS_GAIN = 0.22
CALM_BELL_GAIN = 0.14
COMBAT_PAD_GAIN = 0.22
COMBAT_OSTINATO_GAIN = 0.40
COMBAT_PERC_GAIN = 0.30
COMBAT_BASS_GAIN = 0.24

# 钢琴密度：0 = 全休止，1 = 每个八分音符都有音
CALM_PIANO_DENSITY = 0.42
COMBAT_TICK_SECONDS = 1.0  # 战斗打击间隔

# 混响
REVERB_SECONDS = 3.2
REVERB_RT60 = 2.6
REVERB_WET = 0.38  # 0 = 干声，1 = 全混响

# 环境声
AMBIENCE_SECONDS = 48.0
RAIN_LEVEL = 0.21
DRIP_LEVEL = 0.09
RUMBLE_LEVEL = 0.14
DRIP_PER_SECOND = 0.9

SEED = 20261124  # 换个数字就是另一首


# ============================================================
# 基础工具
# ============================================================

def midi_to_freq(m: float) -> float:
    return 440.0 * 2.0 ** ((m - 69.0) / 12.0)


def fft_filter(x: np.ndarray, lo: float | None = None, hi: float | None = None, order: int = 2) -> np.ndarray:
    """零相位 FFT 滤波（对环形缓冲同样保持周期性）。"""
    n = x.shape[-1]
    freqs = np.fft.rfftfreq(n, 1.0 / SR)
    resp = np.ones_like(freqs)
    if hi is not None:
        resp *= 1.0 / np.sqrt(1.0 + (freqs / hi) ** (2 * order))
    if lo is not None:
        f = np.maximum(freqs, 1e-6)
        resp *= 1.0 / np.sqrt(1.0 + (lo / f) ** (2 * order))
    spec = np.fft.rfft(x, axis=-1) * resp
    return np.fft.irfft(spec, n=n, axis=-1)


def cosine_ramp(n: int) -> np.ndarray:
    """0 → 1 的升余弦，用于无爆音的包络。"""
    if n <= 1:
        return np.ones(max(n, 1))
    t = np.linspace(0.0, 1.0, n)
    return 0.5 - 0.5 * np.cos(np.pi * t)


def envelope(n: int, attack: float, release: float) -> np.ndarray:
    a = min(int(attack * SR), n)
    r = min(int(release * SR), n - a)
    env = np.ones(n)
    if a > 1:
        env[:a] = cosine_ramp(a)
    if r > 1:
        env[n - r:] = cosine_ramp(r)[::-1]
    return env


class Loop:
    """环形缓冲：所有写入都按总长度取模，天然可循环。"""

    def __init__(self, seconds: float):
        self.n = int(round(seconds * SR))
        self.buf = np.zeros((2, self.n), dtype=np.float64)

    def add(self, sig: np.ndarray, start_seconds: float, pan: float = 0.0, gain: float = 1.0) -> None:
        if sig.size == 0:
            return
        if sig.shape[0] != self.n and sig.ndim == 1:
            pass
        start = int(round(start_seconds * SR))
        idx = (np.arange(sig.shape[-1]) + start) % self.n
        # 等功率声像：pan = -1 全左，+1 全右
        theta = (pan + 1.0) * np.pi / 4.0
        self.buf[0, idx] += sig * gain * np.cos(theta)
        self.buf[1, idx] += sig * gain * np.sin(theta)


# ============================================================
# 乐器（都是合成出来的，不需要采样）
# ============================================================

def pad_chord(notes: list[int], seconds: float, fade: float = 1.6, bright: float = 1.0) -> np.ndarray:
    """弦垫：多个正弦泛音叠加 + 轻微失谐，慢起慢落。"""
    n = int(seconds * SR)
    t = np.arange(n) / SR
    out = np.zeros(n)
    for i, note in enumerate(notes):
        f0 = midi_to_freq(note)
        detune = 1.0 + 0.0016 * np.sin(2 * np.pi * (0.07 + 0.013 * i) * t + i * 1.7)
        partials = [(1, 1.0), (2, 0.30 * bright), (3, 0.13 * bright),
                    (4, 0.07 * bright), (5, 0.035 * bright), (6, 0.018 * bright)]
        voice = np.zeros(n)
        for h, amp in partials:
            voice += amp * np.sin(2 * np.pi * f0 * h * t * detune + h * 0.4)
        out += voice / (1.0 + 0.25 * i)
    out /= max(1, len(notes))
    out *= envelope(n, fade, fade)
    return out


def piano_note(freq: float, seconds: float, velocity: float = 0.6) -> np.ndarray:
    """钢琴感：泛音按 1/n 衰减，高次泛音衰减更快，带一点击弦噪声。"""
    n = int(seconds * SR)
    t = np.arange(n) / SR
    out = np.zeros(n)
    for h, amp in [(1, 1.0), (2, 0.48), (3, 0.26), (4, 0.13), (5, 0.07), (6, 0.035), (7, 0.018)]:
        tau = seconds / (0.55 + 0.75 * h)
        out += amp * np.sin(2 * np.pi * freq * h * t * (1.0 + 0.0009 * h * h) + h * 0.6) * np.exp(-t / tau)
    # 击弦瞬态
    click = np.random.default_rng(int(freq) % 100000).standard_normal(n) * np.exp(-t * 900)
    out += 0.035 * fft_filter(click, lo=1200, hi=7000)
    out *= (1.0 - np.exp(-t * 260.0)) * velocity
    return fft_filter(out, hi=9000, order=1)


def bass_note(freq: float, seconds: float, velocity: float = 0.7) -> np.ndarray:
    n = int(seconds * SR)
    t = np.arange(n) / SR
    out = np.sin(2 * np.pi * freq * t) + 0.18 * np.sin(2 * np.pi * freq * 2 * t + 0.5)
    out *= envelope(n, 0.25, min(1.6, seconds * 0.4)) * velocity
    return fft_filter(out, hi=900, order=1)


def bell_note(freq: float, seconds: float, velocity: float = 0.4) -> np.ndarray:
    """钟/铁：FM 合成，非线性泛音。"""
    n = int(seconds * SR)
    t = np.arange(n) / SR
    index = 2.6 * np.exp(-t / 0.9) + 0.35
    mod = np.sin(2 * np.pi * freq * 3.51 * t)
    out = np.sin(2 * np.pi * freq * t + index * mod)
    out *= np.exp(-t / (seconds * 0.42)) * (1.0 - np.exp(-t * 400.0)) * velocity
    return out


def tick(seconds: float = 0.18, velocity: float = 0.5) -> np.ndarray:
    """战斗里的小打击：窄带噪声。"""
    rng = np.random.default_rng(7)
    n = int(seconds * SR)
    t = np.arange(n) / SR
    noise = rng.standard_normal(n) * np.exp(-t * 45.0)
    return fft_filter(noise, lo=700, hi=4200, order=2) * velocity


def thump(seconds: float = 0.45, velocity: float = 0.8) -> np.ndarray:
    """低频鼓点：频率下滑的正弦。"""
    n = int(seconds * SR)
    t = np.arange(n) / SR
    f = 78.0 * np.exp(-t * 5.0) + 42.0
    phase = 2 * np.pi * np.cumsum(f) / SR
    return np.sin(phase) * np.exp(-t * 9.0) * velocity


def drip(seconds: float = 0.18, freq: float = 1400.0, velocity: float = 0.4) -> np.ndarray:
    n = int(seconds * SR)
    t = np.arange(n) / SR
    f = freq * (1.0 + 0.9 * np.exp(-t * 55.0))
    phase = 2 * np.pi * np.cumsum(f) / SR
    out = np.sin(phase) * np.exp(-t * (7.0 / max(seconds, 0.05)))
    # 一点点水泡的共振
    out += 0.35 * np.sin(2 * np.pi * freq * 2.7 * t) * np.exp(-t * 40.0)
    return fft_filter(out, lo=400, hi=6000) * velocity


# ============================================================
# 混响
# ============================================================

def make_ir(rng: np.random.Generator) -> np.ndarray:
    n = int(REVERB_SECONDS * SR)
    t = np.arange(n) / SR
    env = np.exp(-6.9 * t / REVERB_RT60) * (t > 0.012)
    ir = rng.standard_normal((2, n)) * env
    # 高频随时间暗下去，听起来更像大厅
    ir = np.stack([fft_filter(ir[0], hi=5200, order=1), fft_filter(ir[1], hi=5200, order=1)])
    energy = np.sqrt(np.sum(ir ** 2) / 2.0)
    return ir / max(energy, 1e-9)


def apply_reverb(loop: Loop, wet: float = REVERB_WET) -> np.ndarray:
    """循环卷积：混响尾巴绕回开头，接缝处不会突然断。"""
    n = loop.n
    ir = make_ir(np.random.default_rng(SEED + 1))
    ir_padded = np.zeros((2, n))
    ir_padded[:, : ir.shape[1]] = ir
    dry = loop.buf
    spec = np.fft.rfft(dry, axis=-1) * np.fft.rfft(ir_padded, axis=-1)
    wet_sig = np.fft.irfft(spec, n=n, axis=-1)
    # 混响本身能量可能很大，按 RMS 拉回和干声同量级
    rms_dry = np.sqrt(np.mean(dry ** 2)) + 1e-9
    rms_wet = np.sqrt(np.mean(wet_sig ** 2)) + 1e-9
    wet_sig *= rms_dry / rms_wet
    return dry * (1.0 - wet * 0.5) + wet_sig * wet


def finish(sig: np.ndarray, path: str, peak: float = 0.9) -> None:
    """EQ + 软限幅 + 归一化 + 写 16bit WAV。"""
    sig = fft_filter(sig, lo=25.0, hi=15500.0, order=2)
    sig = np.tanh(sig * 1.15) / np.tanh(1.15)
    max_abs = float(np.max(np.abs(sig)))
    if max_abs > 1e-9:
        sig *= peak / max_abs
    data = np.clip(sig, -1.0, 1.0).T
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((data * 32767.0).astype("<i2").tobytes())
    rms = 20 * np.log10(np.sqrt(np.mean(sig ** 2)) + 1e-12)
    print(f"{os.path.basename(path):28s} {sig.shape[-1] / SR:5.1f}s  peak {np.max(np.abs(sig)):.2f}  rms {rms:6.1f} dB  "
          f"{os.path.getsize(path) / 1048576:.1f} MB")


# ============================================================
# 编曲
# ============================================================

def build_calm() -> np.ndarray:
    rng = np.random.default_rng(SEED)
    chord_dur = CALM_CHORD_SECONDS
    total = chord_dur * len(CALM_CHORDS) * CALM_CYCLES
    loop = Loop(total)
    fade = 1.8

    # 弦垫：每个和弦铺满，首尾交叠
    for i in range(int(total / chord_dur)):
        bass, notes = CALM_CHORDS[i % len(CALM_CHORDS)]
        start = i * chord_dur - fade
        loop.add(pad_chord(notes, chord_dur + 2 * fade, fade, bright=0.9),
                 start, gain=CALM_PAD_GAIN)
        loop.add(bass_note(midi_to_freq(bass), chord_dur * 0.92, 0.55),
                 i * chord_dur, gain=CALM_BASS_GAIN)

    # 钢琴动机：每半个和弦换一个节奏型，音符从和弦音里挑
    patterns = [
        [0.5, 2.0, 3.0],
        [0.0, 1.5, 3.5],
        [0.0, 2.0, 4.5, 6.0],
        [1.0, 2.5, 5.0],
        [0.0, 3.0],
        [0.5, 1.5, 4.0, 5.5, 7.0],
    ]
    for i in range(int(total / chord_dur)):
        bass, notes = CALM_CHORDS[i % len(CALM_CHORDS)]
        start = i * chord_dur
        for half in range(2):
            pattern = patterns[int(rng.integers(0, len(patterns)))]
            for offset in pattern:
                if rng.random() > 0.55 + CALM_PIANO_DENSITY * 0.45:
                    continue
                note = int(rng.choice(notes)) + (12 if rng.random() < 0.35 else 0)
                t = start + half * (chord_dur / 2) + offset
                if t >= total:
                    continue
                vel = float(rng.uniform(0.32, 0.62))
                loop.add(piano_note(midi_to_freq(note), float(rng.uniform(2.0, 3.4)), vel),
                         t, pan=float(rng.uniform(-0.45, 0.45)), gain=CALM_PIANO_GAIN)

    # 第二遍加一条高音旋律线（长音），让重复不那么死板
    melody = [76, 74, 72, 71, 69, 72, 74, 76]  # E5 D5 C5 B4 A4 C5 D5 E5
    for k, note in enumerate(melody):
        t = total / 2 + k * (total / 2 / len(melody))
        loop.add(piano_note(midi_to_freq(note), 4.2, 0.34),
                 t, pan=float(np.sin(k) * 0.3), gain=CALM_PIANO_GAIN * 0.8)

    # 第二遍的钟声点缀
    for k, note in enumerate([88, 84, 86, 83]):
        t = total / 2 + k * (chord_dur)
        loop.add(bell_note(midi_to_freq(note), 4.0, 0.4),
                 t + 1.5, pan=float(np.cos(k * 1.3) * 0.5), gain=CALM_BELL_GAIN)

    return apply_reverb(loop)


def build_combat() -> np.ndarray:
    rng = np.random.default_rng(SEED + 7)
    chord_dur = COMBAT_CHORD_SECONDS
    total = chord_dur * len(COMBAT_CHORDS) * COMBAT_CYCLES
    loop = Loop(total)
    fade = 0.7

    for i in range(int(total / chord_dur)):
        bass, notes = COMBAT_CHORDS[i % len(COMBAT_CHORDS)]
        start = i * chord_dur
        loop.add(pad_chord(notes, chord_dur + 2 * fade, fade, bright=1.5),
                 start - fade, gain=COMBAT_PAD_GAIN)
        loop.add(bass_note(midi_to_freq(bass), chord_dur * 0.95, 0.8),
                 start, gain=COMBAT_BASS_GAIN)

        # 八分音符固定音型：根音 / 五度来回，每两小节换一次顺序
        root = midi_to_freq(bass + 12)
        fifth = midi_to_freq(bass + 19)
        steps = int(chord_dur / 0.5)
        for s in range(steps):
            f = root if (s // 2 + i) % 2 == 0 else fifth
            accent = 0.85 if s % 4 == 0 else 0.55
            loop.add(piano_note(f, 0.9, accent),
                     start + s * 0.5, pan=float(np.sin(s * 0.9) * 0.25),
                     gain=COMBAT_OSTINATO_GAIN)

        # 打击：一小节一下闷鼓 + 每拍一下轻 tick
        loop.add(thump(0.5, 0.75), start, gain=COMBAT_PERC_GAIN)
        for s in range(int(chord_dur / COMBAT_TICK_SECONDS)):
            loop.add(tick(0.16, 0.32 + 0.2 * (s % 2)),
                     start + 0.5 + s * COMBAT_TICK_SECONDS,
                     pan=0.15, gain=COMBAT_PERC_GAIN)

        # 高音紧张感：每和弦一个长音
        tension = int(rng.choice(notes)) + 24
        loop.add(piano_note(midi_to_freq(tension), 2.4, 0.2),
                 start + 0.25, pan=-0.5, gain=COMBAT_OSTINATO_GAIN * 0.5)

    return apply_reverb(loop, wet=REVERB_WET * 0.75)


def build_ambience() -> np.ndarray:
    rng = np.random.default_rng(SEED + 13)
    loop = Loop(AMBIENCE_SECONDS)
    n = loop.n
    t = np.arange(n) / SR

    # 雨：宽频噪声 + 缓慢阵风
    rain = rng.standard_normal((2, n))
    rain = fft_filter(rain, lo=250.0, hi=7000.0, order=2)
    gust = (1.0
            + 0.22 * np.sin(2 * np.pi * 0.071 * t + 0.4)
            + 0.13 * np.sin(2 * np.pi * 0.113 * t + 2.1)
            + 0.08 * np.sin(2 * np.pi * 0.037 * t + 4.3))
    loop.buf += rain * gust * RAIN_LEVEL * 0.35

    # 城市低鸣：更低的噪声层
    rumble = rng.standard_normal((2, n))
    rumble = fft_filter(rumble, lo=32.0, hi=160.0, order=2)
    loop.buf += rumble * (0.8 + 0.2 * np.sin(2 * np.pi * 0.021 * t)) * RUMBLE_LEVEL

    # 滴水：随机位置，随机音高
    count = int(AMBIENCE_SECONDS * DRIP_PER_SECOND)
    for _ in range(count):
        start = float(rng.uniform(0, AMBIENCE_SECONDS))
        f = float(rng.uniform(700, 2100))
        loop.add(drip(float(rng.uniform(0.10, 0.22)), f, float(rng.uniform(0.25, 0.6))),
                 start, pan=float(rng.uniform(-0.8, 0.8)), gain=DRIP_LEVEL)

    # 极远处的钟（很轻，给"城市"一点轮廓）
    for k in range(3):
        loop.add(bell_note(midi_to_freq(76 + k * 3), 6.0, 0.18),
                 k * (AMBIENCE_SECONDS / 3) + 7.0,
                 pan=float(np.sin(k * 2.1) * 0.6), gain=0.06)

    out = fft_filter(loop.buf, lo=28.0, hi=15000.0, order=2)
    return out


# ============================================================

def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default="assets/music", help="输出目录")
    ap.add_argument("--only", default=None, choices=["calm", "combat", "ambience"])
    args = ap.parse_args()

    if args.only in (None, "calm"):
        finish(build_calm(), os.path.join(args.out, "tearscity_calm.wav"))
    if args.only in (None, "combat"):
        finish(build_combat(), os.path.join(args.out, "tearscity_combat.wav"))
    if args.only in (None, "ambience"):
        finish(build_ambience(), os.path.join(args.out, "tearscity_ambience.wav"), peak=0.63)  # 峰值更低 = 整体比音乐轻一档


if __name__ == "__main__":
    main()
