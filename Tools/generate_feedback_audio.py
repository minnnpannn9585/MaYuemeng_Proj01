"""Deterministic original sound design. Requires Python 3 + numpy; no downloaded samples.

Run from any directory: python Tools/generate_feedback_audio.py
48 kHz stereo PCM, modal resonances, band-limited air/foley and short stereo reflections.
"""
from pathlib import Path
import json
import uuid
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Resources/Feedback/Audio"
RATE = 48000
TAU = 2 * np.pi


def envelope(t, duration, attack=0.006, release=0.06):
    return np.clip(t / attack, 0, 1) ** 1.4 * np.clip((duration - t) / release, 0, 1) ** 2


def noise(rng, n, low, high):
    """Smooth spectral filter avoids harsh sample-and-hold/white-noise transients."""
    freq = np.fft.rfftfreq(n, 1 / RATE)
    spectrum = np.fft.rfft(rng.normal(size=n))
    window = (1 - np.exp(-(freq / low) ** 4)) * np.exp(-(freq / high) ** 4)
    signal = np.fft.irfft(spectrum * window, n=n)
    return signal / max(np.std(signal), 1e-6) * 0.2


def sweep(t, start, end, duration):
    # Integral of frequency, unlike sin(t * changing_frequency).
    phase = start * t + (end - start) * t * t / (2 * duration)
    return np.sin(TAU * phase)


def modal(t, fundamental, decay=0.2, ceramic=False):
    ratios = [1, 1.59, 2.17, 2.92, 4.07] if ceramic else [1, 2.002, 3.997, 5.02]
    result = np.zeros_like(t)
    for k, ratio in enumerate(ratios):
        result += np.sin(TAU * fundamental * ratio * t) * np.exp(-t / (decay / (1 + k * 0.65))) / (1 + k) ** 1.7
    return result * np.clip(t / 0.002, 0, 1)


def bell(t, start, freq, decay=0.32):
    local = np.maximum(t - start, 0)
    return modal(local, freq, decay) * (t >= start)


def sound(cue, variation):
    rng = np.random.default_rng(1729 + list(CUES).index(cue) * 100 + variation)
    duration = CUES[cue][0]
    t = np.arange(round(duration * RATE)) / RATE
    pitch = [1, 0.966, 1.035][variation - 1]
    air = noise(rng, len(t), 350, 4200)
    earth = noise(rng, len(t), 70, 1400)
    x = np.zeros_like(t)
    wet = 0.08
    if cue in ("Jump", "Land", "Climb", "Drop", "PotImpact"):
        # Dry earthy foley; pots get short, inharmonic ceramic resonances.
        decay = {"Jump": .065, "Land": .075, "Climb": .045, "Drop": .055, "PotImpact": .11}[cue]
        x = earth * np.exp(-t / decay) * 1.4 + air * np.exp(-t / .027) * .28
        if cue in ("Drop", "PotImpact"):
            x += modal(t, (440 if cue == "Drop" else 285) * pitch, .095, True) * .32
            x += sweep(t, 125, 65, duration) * np.exp(-t / .065) * .22
        elif cue == "Land":
            x += sweep(t, 110 * pitch, 55, duration) * np.exp(-t / .058) * .44
        elif cue == "Jump":
            x += air * np.sin(np.pi * np.clip(t / .19, 0, 1)) ** 2 * .75
            x += sweep(t, 240 * pitch, 430 * pitch, duration) * np.exp(-t / .055) * .18
        else:
            x += modal(t, 780 * pitch, .035, True) * .15
    elif cue == "Pickup":
        x = modal(t, 590 * pitch, .09, True) * .26
        x += bell(t, .025, 880, .13) * .18 + air * np.exp(-t / .07) * .5
        wet = .12
    elif cue == "Bounce":
        x = sweep(t, 160 * pitch, 530 * pitch, duration) * np.exp(-t / .12) * .4
        x += air * np.exp(-t / .12) * .6 + bell(t, .045, 783.99, .16) * .2
        wet = .13
    elif cue in ("Collect", "Bloom", "Reveal", "Respawn", "Portal"):
        notes = {
            "Collect": [(0, 1046.5), (.065, 1318.51), (.13, 1567.98), (.205, 2093)],
            "Bloom": [(0, 392), (.09, 587.33), (.18, 783.99), (.26, 1174.66)],
            "Reveal": [(0, 1174.66), (.075, 1567.98), (.15, 2349.32)],
            "Respawn": [(0, 523.25), (.07, 783.99), (.14, 1046.5)],
            "Portal": [(0, 392), (.1, 587.33), (.2, 783.99), (.3, 1174.66), (.4, 1567.98)],
        }[cue]
        for k, (start, hz) in enumerate(notes):
            x += bell(t, start, hz, .25 + k * .025) * (.23 if cue != "Reveal" else .17)
        swell = np.sin(np.pi * np.clip(t / .45, 0, 1)) ** 2
        x += air * swell * (.22 if cue == "Collect" else .42)
        if cue == "Bloom":
            x += earth * np.exp(-t / .14) * .6
        wet = .22
    elif cue == "TimeTravel":
        swell = np.sin(np.pi * np.clip(t / .65, 0, 1)) ** 2
        x = air * swell * 1.4
        x += sweep(t, 180, 720, .65) * swell * .13
        x += sweep(t, 181.3, 723, .65) * swell * .08
        x += bell(t, .32, 783.99, .27) * .22 + bell(t, .39, 1174.66, .3) * .18
        wet = .26
    elif cue == "Trap":
        x = earth * np.exp(-t / .07) + air * np.exp(-t / .035) * .8
        x += sweep(t, 240, 80, duration) * np.exp(-t / .09) * .3
    elif cue == "BatFlee":
        # Fluttering cloth/wing pulses with a quiet chirp, not a piercing alarm.
        for start in (0, .09, .19):
            local = np.maximum(t - start, 0)
            x += air * np.sin(np.pi * np.clip(local / .075, 0, 1)) ** 2 * .9
        x += sweep(t, 2100, 1250, duration) * np.exp(-((t - .08) / .025) ** 2) * .06
    elif cue == "LaserHit":
        x = air * np.exp(-t / .035) * 1.3 + modal(t, 1800 * pitch, .023, True) * .14
    elif cue == "Denied":
        x = bell(t, 0, 293.66, .055) * .26 + bell(t, .075, 261.63, .07) * .23
        wet = .04
    x *= envelope(t, duration, .003, min(.16, duration * .25))
    x -= np.mean(x)
    # Subtle early reflections retain mono compatibility; no phase inversion.
    stereo = np.stack([x.copy(), x.copy()], axis=1)
    for channel in (0, 1):
        for k, delay in enumerate((.029 + channel * .007, .067 - channel * .011, .113 + channel * .013)):
            samples = round(delay * RATE)
            stereo[samples:, channel] += x[:-samples] * wet * (.65 ** k)
    stereo *= envelope(t, duration, .002, .045)[:, None]
    # Uniform headroom; runtime mix supplies cue hierarchy. Peak below -4 dBFS.
    stereo *= 0.60 / max(np.max(np.abs(stereo)), 1e-6)
    stereo[0] = stereo[-1] = 0
    assert np.all(np.isfinite(stereo))
    return stereo


CUES = {
    "Jump": (.30, 3), "Land": (.32, 3), "Climb": (.20, 3), "Pickup": (.40, 2),
    "Drop": (.28, 3), "PotImpact": (.42, 3), "Bounce": (.55, 2), "Collect": (1.05, 1),
    "TimeTravel": (1.20, 1), "Trap": (.38, 2), "Respawn": (.85, 1), "BatFlee": (.40, 2),
    "Reveal": (.90, 1), "Bloom": (1.10, 1), "LaserHit": (.24, 3), "Denied": (.30, 1), "Portal": (1.40, 1)
}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    stats = []
    for cue, (_, variants) in CUES.items():
        for v in range(1, variants + 1):
            data = sound(cue, v)
            path = OUT / f"{cue}_{v}.wav"
            with wave.open(str(path), "wb") as wav:
                wav.setparams((2, 2, RATE, len(data), "NONE", "not compressed"))
                wav.writeframes(np.round(data * 32767).astype("<i2").tobytes())
            meta = Path(str(path) + ".meta")
            if not meta.exists():
                guid = uuid.uuid5(uuid.NAMESPACE_URL, "forest-feedback/" + path.name).hex
                meta.write_text(f"""fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 48000
    compressionFormat: 0
    quality: 1
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {{}}
  forceToMono: 0
  normalize: 0
  ambisonic: 0
  loadInBackground: 0
  preloadAudioData: 1
  assetBundleName:
  assetBundleVariant:
""", encoding="utf-8")
            stats.append({"file": path.name, "seconds": round(len(data) / RATE, 3),
                          "peak_dbfs": round(float(20 * np.log10(np.max(np.abs(data)))), 2),
                          "rms_dbfs": round(float(20 * np.log10(np.sqrt(np.mean(data ** 2)))) , 2)})
    log = ROOT / "Logs/FeedbackQA"
    log.mkdir(parents=True, exist_ok=True)
    (log / "audio-analysis.json").write_text(json.dumps(stats, indent=2), encoding="utf-8")
    preview_order = ["Jump", "Land", "PotImpact", "Bounce", "Collect", "TimeTravel"]
    preview = np.concatenate([part for cue in preview_order
                              for part in (sound(cue, 1), np.zeros((round(.35 * RATE), 2)))])
    with wave.open(str(log / "sound-preview.wav"), "wb") as wav:
        wav.setparams((2, 2, RATE, len(preview), "NONE", "not compressed"))
        wav.writeframes(np.round(preview * 32767).astype("<i2").tobytes())
    print(f"Generated {len(stats)} original stereo WAVs, 48 kHz, peaks below -4 dBFS.")


if __name__ == "__main__":
    main()
