using System.Collections.Generic;
using UnityEngine;

namespace TopClimbing
{
    /// <summary>
    /// Hazır ses dosyası gerektirmeden, çalışma anında basit AudioClip'ler üretir.
    /// Müzik (arpej döngüleri), efektler (blip) ve motor sesi burada oluşturulur.
    /// </summary>
    public static class ProceduralAudio
    {
        private const int SampleRate = 44100;

        // Nota frekansları (Hz)
        private static readonly Dictionary<string, float> Notes = new Dictionary<string, float>
        {
            {"C4",261.63f},{"D4",293.66f},{"E4",329.63f},{"F4",349.23f},{"G4",392.0f},
            {"A4",440.0f},{"B4",493.88f},{"C5",523.25f},{"D5",587.33f},{"E5",659.25f},
            {"F5",698.46f},{"G5",783.99f},
            // Bas oktavı
            {"C2",65.41f},{"D2",73.42f},{"E2",82.41f},{"F2",87.31f},{"G2",98.0f},{"A2",110.0f},{"B2",123.47f},
            {"C3",130.81f},{"D3",146.83f},{"E3",164.81f},{"F3",174.61f},{"G3",196.0f},{"A3",220.0f},{"B3",246.94f}
        };

        private static float Freq(string note) => Notes.TryGetValue(note, out var f) ? f : 0f;

        /// <summary>
        /// Bir melodi (8'lik) + bas (4'lük) hattını karıştırarak dolgun, yumuşak bir parça üretir.
        /// Harmonikli (warm) timbre + attack/release zarfı -> ince chiptune yerine sıcak ses.
        /// </summary>
        public static AudioClip BuildSong(string name, string[] melody, string[] bass, float bpm, float volume = 0.16f)
        {
            float beat = 60f / bpm;                       // 1 vuruş
            int spEighth = Mathf.RoundToInt(beat / 2f * SampleRate); // 8'lik nota uzunluğu
            int total = spEighth * melody.Length;
            float[] data = new float[total];

            // Melodi (8'lik notalar)
            for (int n = 0; n < melody.Length; n++)
                AddTone(data, n * spEighth, spEighth, Freq(melody[n]), volume);

            // Bas (4'lük notalar = 2 melodi notası); kalın, harmonikli kök sesler
            int spQuarter = spEighth * 2;
            for (int n = 0; n < bass.Length; n++)
            {
                int start = n * spQuarter;
                if (start >= total) break;
                AddTone(data, start, Mathf.Min(spQuarter, total - start), Freq(bass[n]), volume * 0.85f);
            }

            // Güvenlik: yumuşak kırpma (clipping engeli)
            for (int i = 0; i < total; i++) data[i] = Mathf.Clamp(data[i], -0.97f, 0.97f);

            var clip = AudioClip.Create(name, total, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Tek bir notayı (temel + yumuşak harmonikler + ADSR zarf) veriye EKLER (mix).</summary>
        private static void AddTone(float[] data, int start, int len, float freq, float vol)
        {
            if (freq <= 0f || len <= 0) return;
            int attack = Mathf.Clamp(len / 8, 64, 1200);
            int release = Mathf.Clamp(len / 3, 256, 4000);
            for (int s = 0; s < len; s++)
            {
                int i = start + s;
                if (i < 0 || i >= data.Length) break;
                float t = (float)s / SampleRate;
                // Sıcak timbre: temel + 2. ve 3. harmonik (azalan)
                float w = Mathf.Sin(2f * Mathf.PI * freq * t)
                        + 0.30f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t)
                        + 0.12f * Mathf.Sin(2f * Mathf.PI * freq * 3f * t);
                w *= 0.55f;
                // Zarf: attack -> sustain -> release (tık sesi yok, yumuşak)
                float env;
                if (s < attack) env = (float)s / attack;
                else if (s > len - release) env = Mathf.Max(0f, (float)(len - s) / release);
                else env = 1f;
                data[i] += w * env * vol;
            }
        }

        /// <summary>Kısa bir efekt sesi (yükselen/düşen ton) üretir.</summary>
        public static AudioClip BuildBlip(string name, float startFreq, float endFreq, float duration, float volume = 0.4f)
        {
            int total = Mathf.RoundToInt(duration * SampleRate);
            float[] data = new float[total];
            for (int s = 0; s < total; s++)
            {
                float prog = (float)s / total;
                float freq = Mathf.Lerp(startFreq, endFreq, prog);
                float t = (float)s / SampleRate;
                float env = Mathf.Pow(1f - prog, 1.5f); // hızlı sönüm
                data[s] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * volume;
            }
            var clip = AudioClip.Create(name, total, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Döngüye uygun, gürültülü motor sesi (testere dalgası + gürültü) üretir.</summary>
        public static AudioClip BuildEngineLoop(string name, float baseFreq = 70f, float duration = 0.5f, float volume = 0.25f)
        {
            int total = Mathf.RoundToInt(duration * SampleRate);
            float[] data = new float[total];
            float phase = 0f;
            for (int s = 0; s < total; s++)
            {
                // Testere dalgası: motor homurtusu
                phase += baseFreq / SampleRate;
                if (phase > 1f) phase -= 1f;
                float saw = (phase * 2f - 1f);
                // Hafif gürültü ekle
                float noise = (Mathf.PerlinNoise(s * 0.05f, 0f) - 0.5f) * 0.4f;
                data[s] = (saw * 0.7f + noise) * volume;
            }
            var clip = AudioClip.Create(name, total, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
