// AudioManager.cs — plays the Kenney UI SFX (loaded from Resources/GKAudio/UI)
// for harvesting, collecting, hovering, and end-of-night, and generates a soft
// looping night ambience procedurally so there is background audio without any
// music file. Self-boots and survives scene loads; all calls are null-safe.

using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    AudioSource ambienceSource;
    AudioSource sfxSource;

    readonly List<AudioClip> switches  = new List<AudioClip>();   // harvest ticks
    readonly List<AudioClip> clicks    = new List<AudioClip>();   // collected / confirm
    readonly List<AudioClip> rollovers = new List<AudioClip>();   // hover / soft
    AudioClip ambienceClip;
    AudioClip swingClip;
    readonly List<AudioClip> ghostVoices = new List<AudioClip>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Instance != null) return;
        var go = new GameObject("~AudioManager");
        go.AddComponent<AudioManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (FindAnyObjectByType<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();

        ambienceSource = gameObject.AddComponent<AudioSource>();
        ambienceSource.loop = true;
        ambienceSource.playOnAwake = false;
        ambienceSource.volume = 0.35f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.volume = 0.7f;

        LoadKenneyClips();

        ambienceClip = BuildNightAmbience();
        swingClip = BuildSwing();

        // Allow real voice files to override (drop .ogg/.wav into Resources/GKAudio/Voices).
        var custom = Resources.LoadAll<AudioClip>("GKAudio/Voices");
        if (custom != null && custom.Length > 0) ghostVoices.AddRange(custom);
        else { ghostVoices.Add(BuildMoan()); ghostVoices.Add(BuildWhisper()); ghostVoices.Add(BuildWail()); }

        ambienceSource.clip = ambienceClip;
        ambienceSource.Play();
    }

    void LoadKenneyClips()
    {
        var all = Resources.LoadAll<AudioClip>("GKAudio/UI");
        foreach (var c in all)
        {
            string n = c.name.ToLowerInvariant();
            if (n.StartsWith("switch"))        switches.Add(c);
            else if (n.StartsWith("click") || n.StartsWith("mouseclick")) clicks.Add(c);
            else if (n.StartsWith("rollover")) rollovers.Add(c);
        }
    }

    static AudioClip Pick(List<AudioClip> list)
    {
        if (list == null || list.Count == 0) return null;
        return list[Random.Range(0, list.Count)];
    }

    void PlayOne(AudioClip c, float vol)
    {
        if (c != null) sfxSource.PlayOneShot(c, vol);
    }

    // ── Static, null-safe API ───────────────────────────────────────────────────
    public static void PlayHarvest(Harvestable.ResourceType type)
    {
        if (!Instance) return;
        Instance.PlayOne(Pick(Instance.switches), 0.8f);
    }

    public static void PlayCollected()
    {
        if (!Instance) return;
        Instance.PlayOne(Pick(Instance.clicks), 0.9f);
    }

    public static void PlayHover()
    {
        if (!Instance) return;
        Instance.PlayOne(Pick(Instance.rollovers), 0.7f);
    }

    public static void PlayClick()
    {
        if (!Instance) return;
        Instance.PlayOne(Pick(Instance.clicks), 0.9f);
    }

    public static void PlayEnd(bool won)
    {
        if (!Instance) return;
        // A little flourish: two confirm clicks for a win, one soft rollover otherwise.
        if (won)
        {
            Instance.PlayOne(Pick(Instance.clicks), 1f);
            Instance.PlayOne(Pick(Instance.switches), 0.8f);
        }
        else
        {
            Instance.PlayOne(Pick(Instance.rollovers), 0.9f);
        }
    }

    // ── Combat ──────────────────────────────────────────────────────────────────
    public static void PlaySwing()
    {
        if (Instance) Instance.PlayOne(Instance.swingClip, 0.6f);
    }

    public static void PlayGhostHit()
    {
        if (Instance) Instance.PlayOne(Pick(Instance.switches), 0.8f);
    }

    public static void PlayGhostBanish()
    {
        if (Instance) Instance.PlayOne(Pick(Instance.clicks), 0.9f);
    }

    public static void PlayHurt()
    {
        if (Instance) Instance.PlayOne(Pick(Instance.switches), 0.9f);
    }

    public static void PlaySoul()
    {
        if (Instance) Instance.PlayOne(Pick(Instance.rollovers), 0.9f);
    }

    public static void PlayGate()
    {
        if (Instance) Instance.PlayOne(Pick(Instance.clicks), 1f);
    }

    public static void ResumeMusic()
    {
        if (!Instance) return;
        Instance.ambienceSource.volume = 0.35f;
        if (!Instance.ambienceSource.isPlaying) Instance.ambienceSource.Play();
    }

    // A short airy "whoosh" for the axe swing.
    AudioClip BuildSwing()
    {
        int sr = 44100;
        int len = (int)(sr * 0.28f);
        float[] buf = new float[len];
        var rng = new System.Random(99);
        float lp = 0f;
        for (int i = 0; i < len; i++)
        {
            float k = i / (float)len;
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (white - lp) * 0.08f;                 // band-ish noise
            float env = Mathf.Sin(k * Mathf.PI);        // swell in and out
            float pitch = 0.6f + k * 0.8f;              // rising whoosh
            buf[i] = lp * env * 0.7f * pitch;
        }
        var clip = AudioClip.Create("swing", len, 1, sr, false);
        clip.SetData(buf, 0);
        return clip;
    }

    // ── Ghost voices ────────────────────────────────────────────────────────────
    public static void PlayGhostVoice()
    {
        if (!Instance || Instance.ghostVoices.Count == 0) return;
        Instance.sfxSource.PlayOneShot(Instance.ghostVoices[Random.Range(0, Instance.ghostVoices.Count)], 0.8f);
    }

    // Low, wavering moan.
    AudioClip BuildMoan()
    {
        int sr = 44100; int len = (int)(sr * 1.8f);
        float[] buf = new float[len]; float phase = 0f;
        for (int i = 0; i < len; i++)
        {
            float k = i / (float)len;
            float vib = 1f + 0.03f * Mathf.Sin(2f * Mathf.PI * 5f * (i / (float)sr));   // vibrato
            float freq = Mathf.Lerp(180f, 130f, k) * vib;
            phase += 2f * Mathf.PI * freq / sr;
            float tone = Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 2f);
            float env = Mathf.Sin(k * Mathf.PI);
            buf[i] = tone * env * 0.35f;
        }
        return FinishVoice("moan", buf, sr);
    }

    // Breathy whisper (filtered noise pulses).
    AudioClip BuildWhisper()
    {
        int sr = 44100; int len = (int)(sr * 1.6f);
        float[] buf = new float[len]; var rng = new System.Random(555); float lp = 0f;
        for (int i = 0; i < len; i++)
        {
            float k = i / (float)len;
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (white - lp) * 0.25f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 3.5f * (i / (float)sr)); // syllable-ish
            float env = Mathf.Sin(k * Mathf.PI);
            buf[i] = lp * pulse * env * 0.5f;
        }
        return FinishVoice("whisper", buf, sr);
    }

    // Rising-then-falling distant wail.
    AudioClip BuildWail()
    {
        int sr = 44100; int len = (int)(sr * 2.2f);
        float[] buf = new float[len]; float phase = 0f;
        for (int i = 0; i < len; i++)
        {
            float k = i / (float)len;
            float shape = Mathf.Sin(k * Mathf.PI);                 // up then down in pitch
            float freq = Mathf.Lerp(260f, 520f, shape) * (1f + 0.02f * Mathf.Sin(2f * Mathf.PI * 6f * (i / (float)sr)));
            phase += 2f * Mathf.PI * freq / sr;
            float tone = Mathf.Sin(phase);
            float env = Mathf.Sin(k * Mathf.PI);
            buf[i] = tone * env * 0.3f;
        }
        return FinishVoice("wail", buf, sr);
    }

    AudioClip FinishVoice(string name, float[] buf, int sr)
    {
        // Cheap tail echo for a haunted, roomy feel.
        int delay = (int)(sr * 0.18f);
        for (int i = delay; i < buf.Length; i++) buf[i] += buf[i - delay] * 0.35f;

        float peak = 0f;
        for (int i = 0; i < buf.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
        if (peak > 0f) { float g = 0.9f / peak; for (int i = 0; i < buf.Length; i++) buf[i] *= g; }

        var clip = AudioClip.Create(name, buf.Length, 1, sr, false);
        clip.SetData(buf, 0);
        return clip;
    }

    // ── Procedural night ambience (wind + faint crickets) ───────────────────────
    AudioClip BuildNightAmbience()
    {
        int sr = 44100;
        int len = sr * 6;                       // 6-second seamless-ish loop
        float[] buf = new float[len];
        var rng = new System.Random(1234);

        // Low wind: filtered noise (simple one-pole low-pass).
        float lp = 0f;
        for (int i = 0; i < len; i++)
        {
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (white - lp) * 0.02f;          // low-pass -> wind rumble
            float wind = lp * 0.5f;

            // Slow amplitude swell so the wind breathes.
            float swell = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * (i / (float)len) * 2f);

            buf[i] = wind * swell;
        }

        // Faint intermittent "crickets": short high chirps sprinkled in.
        for (int c = 0; c < 40; c++)
        {
            int start = rng.Next(0, len - 2000);
            float freq = 2600f + (float)rng.NextDouble() * 800f;
            for (int i = 0; i < 1200 && start + i < len; i++)
            {
                float t = i / (float)sr;
                float env = Mathf.Sin(i / 1200f * Mathf.PI);
                buf[start + i] += Mathf.Sin(2f * Mathf.PI * freq * t) * 0.05f * env;
            }
        }

        // Normalize to avoid clipping.
        float peak = 0f;
        for (int i = 0; i < len; i++) peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
        if (peak > 0f) { float g = 0.9f / peak; for (int i = 0; i < len; i++) buf[i] *= g; }

        var clip = AudioClip.Create("night_ambience", len, 1, sr, false);
        clip.SetData(buf, 0);
        return clip;
    }
}
