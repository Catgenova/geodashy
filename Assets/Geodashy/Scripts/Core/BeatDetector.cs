using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>
    /// Guesses a song's tempo and first-beat offset from its samples: an onset-energy envelope, an
    /// autocorrelation over 70-190 BPM, then the beat phase that lines up with the most onsets. Also builds
    /// the peak waveform used by the timeline strip. Needs a clip whose samples can be read (decompressed on
    /// load, which is how imported and shipped songs are set up).
    /// </summary>
    public static class BeatDetector
    {
        const int Hop = 512;

        static float[] Mono(AudioClip clip, float maxSeconds)
        {
            int channels = Mathf.Max(1, clip.channels);
            int frames = Mathf.Min(clip.samples, Mathf.RoundToInt(maxSeconds * clip.frequency));
            if (frames <= 0) return null;
            var raw = new float[frames * channels];
            try
            {
                if (!clip.GetData(raw, 0)) return null;
            }
            catch (System.Exception)
            {
                return null;
            }
            var mono = new float[frames];
            for (int i = 0; i < frames; i++)
            {
                float s = 0f;
                for (int c = 0; c < channels; c++) s += raw[i * channels + c];
                mono[i] = s / channels;
            }
            return mono;
        }

        static float[] Onsets(float[] mono)
        {
            int n = mono.Length / Hop;
            var energy = new float[n];
            for (int i = 0; i < n; i++)
            {
                float e = 0f;
                int o = i * Hop;
                for (int k = 0; k < Hop; k++) { float v = mono[o + k]; e += v * v; }
                energy[i] = Mathf.Sqrt(e / Hop);
            }
            var onset = new float[n];
            float mean = 0f;
            for (int i = 1; i < n; i++)
            {
                onset[i] = Mathf.Max(0f, energy[i] - energy[i - 1]);
                mean += onset[i];
            }
            mean /= Mathf.Max(1, n);
            for (int i = 0; i < n; i++) onset[i] = Mathf.Max(0f, onset[i] - mean * 0.5f);
            return onset;
        }

        /// <summary>Estimates BPM and the offset (seconds) of the first beat. False when the samples cannot be read.</summary>
        public static bool Analyse(AudioClip clip, out float bpm, out float offset)
        {
            bpm = 120f;
            offset = 0f;
            if (clip == null) return false;
            var mono = Mono(clip, 60f);
            if (mono == null || mono.Length < clip.frequency * 4) return false;
            var onset = Onsets(mono);
            float hopSeconds = Hop / (float)clip.frequency;
            float best = -1f;
            for (float cand = 70f; cand <= 190f; cand += 0.5f)
            {
                float period = 60f / cand / hopSeconds;
                int lag = Mathf.RoundToInt(period), lag2 = Mathf.RoundToInt(period * 2f);
                float score = 0f;
                for (int i = 0; i + lag2 < onset.Length; i++) score += onset[i] * (onset[i + lag] + 0.5f * onset[i + lag2]);
                // mild preference for dance tempos so the half/double-time twins do not win by noise
                float pref = 1f - Mathf.Abs(cand - 130f) / 400f;
                score *= pref;
                if (score > best)
                {
                    best = score;
                    bpm = cand;
                }
            }
            // phase: the shift that puts the beat grid over the loudest onsets in the first 20 seconds
            float p = 60f / bpm / hopSeconds;
            int span = Mathf.Min(onset.Length, Mathf.RoundToInt(20f / hopSeconds));
            float bestPhase = 0f, bestSum = -1f;
            int steps = Mathf.Max(1, Mathf.RoundToInt(p));
            for (int ph = 0; ph < steps; ph++)
            {
                float sum = 0f;
                for (float t = ph; t < span; t += p)
                {
                    int i = Mathf.RoundToInt(t);
                    if (i < onset.Length) sum += onset[i];
                }
                if (sum > bestSum) { bestSum = sum; bestPhase = ph; }
            }
            offset = bestPhase * hopSeconds;
            bpm = Mathf.Round(bpm * 2f) / 2f;
            return true;
        }

        /// <summary>Peak amplitude (0..1) per bin across the whole clip, for drawing.</summary>
        public static float[] Waveform(AudioClip clip, int bins)
        {
            var result = new float[Mathf.Max(1, bins)];
            if (clip == null) return result;
            var mono = Mono(clip, 3600f);
            if (mono == null || mono.Length == 0) return result;
            int per = Mathf.Max(1, mono.Length / result.Length);
            float peak = 0.0001f;
            for (int b = 0; b < result.Length; b++)
            {
                float m = 0f;
                int start = b * per, end = Mathf.Min(mono.Length, start + per);
                for (int i = start; i < end; i += 4) m = Mathf.Max(m, Mathf.Abs(mono[i]));
                result[b] = m;
                peak = Mathf.Max(peak, m);
            }
            for (int b = 0; b < result.Length; b++) result[b] /= peak;
            return result;
        }
    }
}
