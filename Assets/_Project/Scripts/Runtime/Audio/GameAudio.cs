using UnityEngine;

namespace DemolitionBuster.Audio
{
    /// <summary>
    /// Sound effects and haptics. The clips are synthesized at startup (noise, sine sweeps, arpeggios),
    /// like the rest of the content, so the repo stays self-contained. To use real samples, replace
    /// the generated clips with serialized AudioClip fields.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;

        [SerializeField, Range(0f, 1f)] private float _volume = 0.7f;

        private AudioSource _source;
        private AudioClip _hit, _coin, _death, _milestone, _revive;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;

            _hit = Synth("Hit", 0.14f, (t, n) => Decay(t, 30f) * (0.6f * Sine(t, 70f + 60f * Decay(t, 40f)) + 0.5f * n));
            _coin = Synth("Coin", 0.18f, (t, _) => Decay(t, 14f) * 0.35f * Square(t, t < 0.06f ? 988f : 1319f));
            _death = Synth("Death", 0.6f, (t, n) => Decay(t, 5f) * (0.5f * Square(t, Mathf.Lerp(330f, 60f, t / 0.6f)) * 0.5f + 0.4f * n * Decay(t, 12f)));
            _milestone = Synth("Milestone", 0.5f, (t, _) => 0.3f * Square(t, Arpeggio(t, 0.09f, 523f, 659f, 784f, 1047f)) * Decay(t % 0.09f, 8f));
            _revive = Synth("Revive", 0.35f, (t, _) => 0.35f * Sine(t, Mathf.Lerp(220f, 880f, t / 0.35f)) * (1f - t / 0.35f));
        }

        public void PlayHit() => Play(_hit, Random.Range(0.9f, 1.1f));
        public void PlayCoin() => Play(_coin);
        public void PlayMilestone() { Play(_milestone); Vibrate(); }
        public void PlayRevive() => Play(_revive);
        public void PlayDeath() { Play(_death); Vibrate(); }

        private void Play(AudioClip clip, float pitch = 1f)
        {
            _source.pitch = pitch;
            _source.PlayOneShot(clip, _volume);
        }

        private static void Vibrate()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate(); // ponytail: long buzz only, native light/medium impacts need a haptics plugin
#endif
        }

        // ---- Tiny synthesizer ------------------------------------------------------------------

        private delegate float Wave(float time, float noise);

        private static AudioClip Synth(string name, float duration, Wave wave)
        {
            var samples = new float[Mathf.CeilToInt(duration * SampleRate)];
            var random = new System.Random(name.GetHashCode()); // deterministic: same sound every launch
            for (int i = 0; i < samples.Length; i++)
            {
                float noise = (float)random.NextDouble() * 2f - 1f;
                samples[i] = Mathf.Clamp(wave(i / (float)SampleRate, noise), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Sine(float t, float frequency) => Mathf.Sin(2f * Mathf.PI * frequency * t);
        private static float Square(float t, float frequency) => Sine(t, frequency) >= 0f ? 1f : -1f;
        private static float Decay(float t, float speed) => Mathf.Exp(-speed * t);

        private static float Arpeggio(float t, float noteLength, params float[] notes) =>
            notes[Mathf.Min(notes.Length - 1, (int)(t / noteLength))];
    }
}
