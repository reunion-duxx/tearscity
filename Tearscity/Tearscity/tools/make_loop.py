"""
把一段普通音频（比如 Suno 导出的曲子）做成「能无缝循环」的 wav。

原理：把尾部最后 `--fade` 秒用等功率交叉淡化叠到开头 `--fade` 秒上，
    输出长度 = 原长 - fade。这样播到头再回到开头时，听到的是连续的尾音，不会有「咔」的断点。

用法：
    python tools/make_loop.py suno_raw.wav -o tearscity_calm.wav --fade 2.0
    python tools/make_loop.py suno_raw.wav -o tearscity_combat.wav --fade 1.5 --trim -52
    python tools/make_loop.py suno_raw.wav -o trial.wav --length 90 --peak -1.5
    # 挑中间某一段（比如旋律没那么强的段落），并把它「雾化」成氛围：
    python tools/make_loop.py raw.wav -o calm.wav --start 42 --length 40 --lowpass 5000 --reverb 0.45

输入支持 wav 和 mp3 / m4a / flac：
    - wav（8/16/24-bit 或 32-bit float）用哪个 python 都能跑；
    - mp3 等压缩格式需要 PyAV 解码，用 **ComfyUI 自带的 python** 跑就行（它装了 av）：

      "C:\\Users\\ASUS\\Downloads\\ComfyUI_windows_portable\\python_embeded\\python.exe" ^
          tools\\make_loop.py mureka.mp3 -o tearscity_calm.wav --length 60

    如果机器上装了 ffmpeg，也可以先 `ffmpeg -i in.mp3 in.wav` 转一下，再用普通 python 跑。
"""

from __future__ import annotations

import argparse
import os
import wave

import numpy as np


def read_wav(path: str) -> tuple[np.ndarray, int]:
    with wave.open(path, "rb") as w:
        channels = w.getnchannels()
        width = w.getsampwidth()
        rate = w.getframerate()
        frames = w.getnframes()
        raw = w.readframes(frames)

    if width == 2:
        data = np.frombuffer(raw, dtype="<i2").astype(np.float64) / 32768.0
    elif width == 3:
        b = np.frombuffer(raw, dtype=np.uint8).reshape(-1, 3).astype(np.int32)
        v = b[:, 0] | (b[:, 1] << 8) | (b[:, 2] << 16)
        v = np.where(v >= 1 << 23, v - (1 << 24), v)
        data = v.astype(np.float64) / float(1 << 23)
    elif width == 1:
        data = (np.frombuffer(raw, dtype=np.uint8).astype(np.float64) - 128.0) / 128.0
    else:
        raise SystemExit(f"不支持的采样位宽 {width * 8} bit；请在 Audacity 里另存为 16-bit PCM wav")

    return data.reshape(-1, channels), rate


def decode_with_av(path: str) -> tuple[np.ndarray, int]:
    """用 PyAV 解码 mp3 / m4a / flac 等格式（PyAV 自带 ffmpeg 解码器）。"""
    try:
        import av
    except ImportError as exc:
        raise SystemExit(
            "这个格式需要 PyAV 解码：请用 ComfyUI 自带的 python 运行，"
            "或者先用 Audacity 把文件导出成 wav。"
        ) from exc

    with av.open(path) as container:
        stream = next((s for s in container.streams if s.type == "audio"), None)
        if stream is None:
            raise SystemExit(f"这条文件里没有音频流：{path}")
        rate = stream.codec_context.sample_rate
        channels = stream.codec_context.channels
        parts = []
        for frame in container.decode(stream):
            array = frame.to_ndarray()
            if frame.format.is_planar:
                array = array.T
            else:
                array = array.reshape(-1, channels)
            if array.dtype.kind in "iu":
                array = array.astype(np.float64) / float(np.iinfo(array.dtype).max)
            parts.append(array.astype(np.float64))

    if not parts:
        raise SystemExit(f"没解出任何音频：{path}")

    return np.concatenate(parts, axis=0), rate


def read_audio(path: str) -> tuple[np.ndarray, int]:
    """wav 走内置读取，其它格式走 PyAV。"""
    if os.path.splitext(path)[1].lower() in (".wav", ".wave"):
        return read_wav(path)
    return decode_with_av(path)


def write_wav(path: str, data: np.ndarray, rate: int) -> None:
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    pcm = (np.clip(data, -1.0, 1.0) * 32767.0).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(data.shape[1])
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(pcm.tobytes())


def trim_silence(data: np.ndarray, threshold_db: float) -> np.ndarray:
    threshold = 10.0 ** (threshold_db / 20.0)
    loud = np.max(np.abs(data), axis=1) > threshold
    if not loud.any():
        return data
    first = int(np.argmax(loud))
    last = int(len(loud) - np.argmax(loud[::-1]))
    # 留 50ms 余量，别把头尾的混响尾巴剪掉
    pad = int(0.05 * 44100)
    return data[max(0, first - pad): min(len(data), last + pad)]


def fft_lowpass(data: np.ndarray, rate: int, cutoff: float) -> np.ndarray:
    """零相位低通：把主旋律的齿音、泛音磨掉，声音变「糊」一点，反而更氛围。"""
    freqs = np.fft.rfftfreq(data.shape[0], 1.0 / rate)
    resp = 1.0 / np.sqrt(1.0 + (freqs / cutoff) ** 4)
    spec = np.fft.rfft(data, axis=0) * resp[:, None]
    return np.fft.irfft(spec, n=data.shape[0], axis=0)


def add_reverb(data: np.ndarray, rate: int, amount: float, seconds: float = 3.0) -> np.ndarray:
    """循环卷积混响：尾巴绕回开头，不会破坏循环。amount 0~1。"""
    n = data.shape[0]
    length = int(seconds * rate)
    t = np.arange(length) / rate
    env = np.exp(-6.9 * t / 2.4) * (t > 0.01)
    rng = np.random.default_rng(1234)
    ir = rng.standard_normal((length, data.shape[1])) * env[:, None]
    ir *= 1.0 / (np.sqrt(np.sum(ir ** 2) / data.shape[1]) + 1e-9)

    padded = np.zeros((n, data.shape[1]))
    padded[:length] = ir
    wet = np.fft.irfft(np.fft.rfft(data, axis=0) * np.fft.rfft(padded, axis=0), n=n, axis=0)

    rms_dry = np.sqrt(np.mean(data ** 2)) + 1e-9
    rms_wet = np.sqrt(np.mean(wet ** 2)) + 1e-9
    wet *= rms_dry / rms_wet
    return data * (1.0 - amount * 0.5) + wet * amount


def make_loop(data: np.ndarray, rate: int, fade_seconds: float) -> np.ndarray:
    total = data.shape[0]
    fade = int(fade_seconds * rate)
    if fade <= 1 or fade * 2 >= total:
        return data

    loop_len = total - fade
    out = data[:loop_len].copy()

    # 等功率交叉淡化：开头先由「尾段」主导，慢慢过渡到「头段」，
    # 于是 out[-1] → out[0] 的衔接处是同一段声音的延续。
    theta = np.linspace(0.0, np.pi / 2.0, fade)[:, None]
    tail = data[loop_len: loop_len + fade]
    head = data[:fade]
    out[:fade] = tail * np.cos(theta) + head * np.sin(theta)
    return out


def main() -> None:
    ap = argparse.ArgumentParser(description="把 wav 做成无缝循环")
    ap.add_argument("input", help="输入 wav 路径")
    ap.add_argument("-o", "--output", required=True, help="输出 wav 路径")
    ap.add_argument("--fade", type=float, default=2.0, help="交叉淡化长度（秒），默认 2.0")
    ap.add_argument("--start", type=float, default=0.0, help="从第 N 秒开始取（用来跳过太旋律化的前奏），默认 0")
    ap.add_argument("--length", type=float, default=None, help="只取 N 秒（默认整首）")
    ap.add_argument("--trim", type=float, default=None, help="自动剪掉首尾静音，阈值 dBFS，例如 -50")
    ap.add_argument("--lowpass", type=float, default=None, help="低通截止频率 Hz，例如 5000（降低旋律存在感）")
    ap.add_argument("--reverb", type=float, default=0.0, help="加混响 0~1，例如 0.4（把旋律糊进空间里）")
    ap.add_argument("--peak", type=float, default=-1.0, help="归一化峰值 dBFS，默认 -1.0")
    ap.add_argument("--rms", type=float, default=None,
                    help="按 RMS 统一响度 dBFS（例如 -18），给多首曲子对齐音量用；设了就用它代替 --peak")
    args = ap.parse_args()

    data, rate = read_audio(args.input)
    original = data.shape[0] / rate

    if args.start > 0:
        data = data[int(args.start * rate):]

    if args.trim is not None:
        data = trim_silence(data, args.trim)

    if args.length is not None:
        data = data[: int(args.length * rate)]

    if args.lowpass is not None:
        data = fft_lowpass(data, rate, args.lowpass)

    if args.reverb > 0:
        data = add_reverb(data, rate, min(args.reverb, 1.0))

    if data.shape[0] < int(args.fade * 2 * rate):
        raise SystemExit(f"素材太短（{data.shape[0] / rate:.1f}s），至少要比 2×fade 长")

    loop = make_loop(data, rate, args.fade)

    if args.rms is not None:
        current = float(np.sqrt(np.mean(loop ** 2))) or 1e-9
        loop *= (10.0 ** (args.rms / 20.0)) / current
        ceiling = 10.0 ** (args.peak / 20.0)
        peak_now = float(np.max(np.abs(loop))) or 1e-9
        if peak_now > ceiling:
            loop *= ceiling / peak_now
    else:
        peak = float(np.max(np.abs(loop))) or 1.0
        loop *= (10.0 ** (args.peak / 20.0)) / peak

    write_wav(args.output, loop, rate)

    seam = float(np.max(np.abs(loop[0] - loop[-1])))
    rms_out = 20 * np.log10(np.sqrt(np.mean(loop ** 2)) + 1e-12)
    print(f"输入 {os.path.basename(args.input)}  {original:.1f}s")
    print(f"输出 {os.path.basename(args.output)}  {loop.shape[0] / rate:.1f}s  "
          f"{loop.shape[1]}ch {rate}Hz  RMS {rms_out:.1f} dBFS  "
          f"峰值 {20 * np.log10(np.max(np.abs(loop))):.1f} dBFS  接缝差 {seam:.4f}")


if __name__ == "__main__":
    main()
