using UnityEngine;

namespace SnowRunner
{
    /// <summary>Generates all sound effects and music procedurally (no audio assets needed).</summary>
    public class AudioManager : MonoBehaviour
    {
        const int Rate = 44100;
        static AudioManager instance;

        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        AudioSource sfx, music;
        AudioClip coin, power, jump, slide, crash, click, loop;

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);

            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = 0.35f;

            coin = Tone("coin", new[] { 988f, 1319f }, 0.09f, 0.35f);
            power = Tone("power", new[] { 523f, 659f, 784f, 1047f }, 0.08f, 0.4f);
            jump = Sweep("jump", 280f, 560f, 0.18f, 0.3f);
            slide = Noise("slide", 0.22f, 0.18f);
            crash = Noise("crash", 0.5f, 0.55f);
            click = Tone("click", new[] { 660f }, 0.05f, 0.3f);
            loop = BuildMusic();
            ApplySound();
        }

        public bool SoundOn => SaveData.SoundOn;

        public void SetSound(bool on)
        {
            SaveData.SoundOn = on;
            ApplySound();
        }

        void ApplySound()
        {
            AudioListener.volume = SaveData.SoundOn ? 1f : 0f;
        }

        public void PlayMusic()
        {
            if (music == null || loop == null) return;
            if (music.clip != loop) music.clip = loop;
            if (!music.isPlaying) music.Play();
        }

        public void StopMusic() { if (music != null) music.Stop(); }

        void Play(AudioClip c) { if (sfx != null && c != null) sfx.PlayOneShot(c); }
        public void PlayCoin() => Play(coin);
        public void PlayPower() => Play(power);
        public void PlayJump() => Play(jump);
        public void PlaySlide() => Play(slide);
        public void PlayCrash() => Play(crash);
        public void PlayClick() => Play(click);

        // ---------- synthesis ----------
        static AudioClip Make(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        static AudioClip Tone(string name, float[] freqs, float noteLen, float vol)
        {
            int per = (int)(noteLen * Rate);
            var d = new float[per * freqs.Length];
            for (int n = 0; n < freqs.Length; n++)
                for (int i = 0; i < per; i++)
                {
                    float t = i / (float)Rate;
                    float env = 1f - i / (float)per;
                    d[n * per + i] = Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * env * vol;
                }
            return Make(name, d);
        }

        static AudioClip Sweep(string name, float f0, float f1, float len, float vol)
        {
            int count = (int)(len * Rate);
            var d = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float k = i / (float)count;
                float f = Mathf.Lerp(f0, f1, k);
                phase += 2f * Mathf.PI * f / Rate;
                d[i] = Mathf.Sin(phase) * (1f - k) * vol;
            }
            return Make(name, d);
        }

        static AudioClip Noise(string name, float len, float vol)
        {
            int count = (int)(len * Rate);
            var d = new float[count];
            var rng = new System.Random(7);
            for (int i = 0; i < count; i++)
            {
                float k = i / (float)count;
                d[i] = ((float)rng.NextDouble() * 2f - 1f) * (1f - k) * vol;
            }
            return Make(name, d);
        }

        static AudioClip BuildMusic()
        {
            // Gentle pentatonic loop: 16 notes x 0.5s = 8 seconds.
            float[] notes = { 392f, 440f, 523f, 587f, 659f, 587f, 523f, 440f,
                              392f, 523f, 659f, 784f, 659f, 523f, 440f, 392f };
            float[] bass = { 130.8f, 130.8f, 146.8f, 146.8f, 164.8f, 164.8f, 146.8f, 130.8f };
            int per = Rate / 2;
            var d = new float[per * notes.Length];
            for (int n = 0; n < notes.Length; n++)
            {
                float b = bass[n / 2];
                for (int i = 0; i < per; i++)
                {
                    float t = i / (float)Rate;
                    float env = Mathf.Exp(-3.5f * t);
                    float lead = Mathf.Sin(2f * Mathf.PI * notes[n] * t) * env * 0.22f;
                    float low = Mathf.Sin(2f * Mathf.PI * b * (n * 0.5f + t)) * 0.12f;
                    d[n * per + i] = lead + low;
                }
            }
            return Make("music", d);
        }
    }
}
