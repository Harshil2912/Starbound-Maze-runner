using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    const string PrefMusic = "MR_MusicEnabled";
    const string PrefSfx = "MR_SfxEnabled";
    const int SampleRate = 24000;

    static GameAudioManager _instance;
    public static GameAudioManager Instance
    {
        get
        {
            if (_instance == null) EnsureCreated();
            return _instance;
        }
    }

    AudioSource bgmSource;
    AudioSource[] sfxPool;
    int sfxPoolIndex;

    AudioClip bgmPeaceful;
    AudioClip sfxMoveStep;
    AudioClip sfxJump;
    AudioClip sfxLand;
    AudioClip sfxWallBump;
    AudioClip sfxStopBrake;
    AudioClip sfxFallVoid;
    AudioClip sfxLavaBurn;
    AudioClip sfxLaserHit;
    AudioClip sfxLaserWarn;
    AudioClip sfxSpikeSnap;
    AudioClip sfxTurretFire;
    AudioClip sfxDamageGeneric;
    AudioClip sfxShieldPickup;
    AudioClip sfxHeartPickup;
    AudioClip sfxCorePickup;
    AudioClip sfxGateOpen;
    AudioClip sfxCheckpoint;
    AudioClip sfxTeleporter;
    AudioClip sfxSwitchPlate;
    AudioClip sfxCrumble;
    AudioClip sfxDeath;
    AudioClip sfxLevelComplete;
    AudioClip sfxUIClick;
    AudioClip sfxARPlace;

    bool musicEnabled = true;
    bool sfxEnabled = true;

    float lastStepSoundTime = -10f;
    float lastBumpSoundTime = -10f;
    float lastSpikeSoundTime = -10f;
    float lastLaserWarnSoundTime = -10f;
    float lastTurretSoundTime = -10f;

    public bool MusicEnabled
    {
        get { return musicEnabled; }
        set
        {
            musicEnabled = value;
            PlayerPrefs.SetInt(PrefMusic, musicEnabled ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMusicState();
        }
    }

    public bool SfxEnabled
    {
        get { return sfxEnabled; }
        set
        {
            sfxEnabled = value;
            PlayerPrefs.SetInt(PrefSfx, sfxEnabled ? 1 : 0);
            PlayerPrefs.Save();
            if (!sfxEnabled) StopAllSfx();
        }
    }

    public void StopAllSfx()
    {
        if (sfxPool == null) return;
        for (int i = 0; i < sfxPool.Length; i++)
        {
            if (sfxPool[i] != null && sfxPool[i].isPlaying)
                sfxPool[i].Stop();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInitialize()
    {
        EnsureCreated();
    }

    public static void EnsureCreated()
    {
        if (_instance != null) return;
        var existing = FindFirstObjectByType<GameAudioManager>();
        if (existing != null)
        {
            _instance = existing;
            return;
        }
        var go = new GameObject("GameAudioManager");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<GameAudioManager>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);

        musicEnabled = PlayerPrefs.GetInt(PrefMusic, 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt(PrefSfx, 1) == 1;

        BuildAudioSources();
        SynthesizeAllClips();
        ApplyMusicState();
    }

    void BuildAudioSources()
    {
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;
        bgmSource.volume = 0.28f;
        bgmSource.priority = 64;

        sfxPool = new AudioSource[6];
        for (int i = 0; i < sfxPool.Length; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.loop = false;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = 1f;
            src.priority = 128;
            sfxPool[i] = src;
        }
    }

    void ApplyMusicState()
    {
        if (bgmSource == null) return;
        bgmSource.clip = bgmPeaceful;
        if (musicEnabled)
        {
            bgmSource.volume = 0.28f;
            if (!bgmSource.isPlaying && bgmPeaceful != null) bgmSource.Play();
        }
        else
        {
            if (bgmSource.isPlaying) bgmSource.Pause();
        }
    }

    void PlayClip(AudioClip clip, float volume = 0.7f, float pitchMin = 1f, float pitchMax = 1f)
    {
        if (!sfxEnabled || clip == null || sfxPool == null) return;
        AudioSource src = sfxPool[sfxPoolIndex];
        sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Length;
        src.pitch = (Mathf.Approximately(pitchMin, pitchMax)) ? pitchMin : Random.Range(pitchMin, pitchMax);
        src.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // ---------------- Public Gameplay Triggers ----------------

    public void PlayCubeStep()
    {
        float now = Time.unscaledTime;
        if (now - lastStepSoundTime < 0.23f) return;
        lastStepSoundTime = now;
        PlayClip(sfxMoveStep, 0.24f, 0.94f, 1.07f);
    }

    public void PlayJump() => PlayClip(sfxJump, 0.52f, 0.97f, 1.04f);
    public void PlayLand() => PlayClip(sfxLand, 0.36f, 0.94f, 1.05f);

    public void PlayWallBump()
    {
        float now = Time.unscaledTime;
        if (now - lastBumpSoundTime < 0.18f) return;
        lastBumpSoundTime = now;
        PlayClip(sfxWallBump, 0.55f, 0.90f, 1.08f);
    }

    public void PlayStopBrake() => PlayClip(sfxStopBrake, 0.48f, 0.98f, 1.02f);
    public void PlayFallVoid() => PlayClip(sfxFallVoid, 0.60f, 0.96f, 1.04f);
    public void PlayDamageLava() => PlayClip(sfxLavaBurn, 0.68f, 0.95f, 1.05f);
    public void PlayDamageLaser() => PlayClip(sfxLaserHit, 0.64f, 0.95f, 1.06f);
    public void PlayDamageGeneric() => PlayClip(sfxDamageGeneric, 0.60f, 0.94f, 1.05f);

    public void PlayLaserWarn()
    {
        float now = Time.unscaledTime;
        if (now - lastLaserWarnSoundTime < 0.28f) return;
        lastLaserWarnSoundTime = now;
        PlayClip(sfxLaserWarn, 0.28f, 0.95f, 1.05f);
    }

    public void PlaySpikeSnap()
    {
        float now = Time.unscaledTime;
        if (now - lastSpikeSoundTime < 0.16f) return;
        lastSpikeSoundTime = now;
        PlayClip(sfxSpikeSnap, 0.42f, 0.94f, 1.06f);
    }

    public void PlayTurretFire()
    {
        float now = Time.unscaledTime;
        if (now - lastTurretSoundTime < 0.12f) return;
        lastTurretSoundTime = now;
        PlayClip(sfxTurretFire, 0.36f, 0.93f, 1.07f);
    }

    public void PlayShieldPickup() => PlayClip(sfxShieldPickup, 0.62f, 1f, 1f);
    public void PlayHeartPickup() => PlayClip(sfxHeartPickup, 0.62f, 1f, 1f);
    public void PlayCorePickup() => PlayClip(sfxCorePickup, 0.64f, 0.98f, 1.04f);
    public void PlayGateOpen() => PlayClip(sfxGateOpen, 0.65f, 1f, 1f);
    public void PlayCheckpoint() => PlayClip(sfxCheckpoint, 0.60f, 1f, 1f);
    public void PlayTeleporter() => PlayClip(sfxTeleporter, 0.58f, 0.98f, 1.03f);
    public void PlaySwitchPlate() => PlayClip(sfxSwitchPlate, 0.52f, 0.97f, 1.04f);
    public void PlayCrumble() => PlayClip(sfxCrumble, 0.50f, 0.92f, 1.06f);
    public void PlayDeath() => PlayClip(sfxDeath, 0.68f, 1f, 1f);
    public void PlayLevelComplete() => PlayClip(sfxLevelComplete, 0.68f, 1f, 1f);
    public void PlayUIClick() => PlayClip(sfxUIClick, 0.38f, 0.97f, 1.03f);
    public void PlayARPlace() => PlayClip(sfxARPlace, 0.60f, 1f, 1f);

    // ---------------- Procedural Synthesis Helpers ----------------

    void SynthesizeAllClips()
    {
        bgmPeaceful = CreatePeacefulBgmClip();
        sfxMoveStep = CreateSweepClip("SFX_Step", 0.065f, 210f, 260f, 0.35f, fadeAttack: 0.006f, harmonic: 0.25f);
        sfxJump = CreateSweepClip("SFX_Jump", 0.16f, 260f, 640f, 0.65f, fadeAttack: 0.008f, harmonic: 0.25f);
        sfxLand = CreateImpactClip("SFX_Land", 0.10f, 145f, 58f, 0.48f, noiseMix: 0.15f);
        sfxWallBump = CreateImpactClip("SFX_Bump", 0.12f, 175f, 52f, 0.68f, noiseMix: 0.32f);
        sfxStopBrake = CreateDoubleToneClip("SFX_Stop", 0.14f, 440f, 210f, 0.55f);
        sfxFallVoid = CreateImpactClip("SFX_FallVoid", 0.42f, 480f, 95f, 0.55f, noiseMix: 0.35f);
        sfxLavaBurn = CreateLavaClip("SFX_Lava", 0.36f);
        sfxLaserHit = CreateLaserHitClip("SFX_LaserHit", 0.28f);
        sfxLaserWarn = CreateSweepClip("SFX_LaserWarn", 0.10f, 760f, 1020f, 0.40f, fadeAttack: 0.01f, harmonic: 0.15f);
        sfxSpikeSnap = CreateSpikeSnapClip("SFX_SpikeSnap", 0.16f);
        sfxTurretFire = CreateSweepClip("SFX_TurretFire", 0.14f, 760f, 155f, 0.52f, fadeAttack: 0.004f, harmonic: 0.30f);
        sfxDamageGeneric = CreateImpactClip("SFX_Damage", 0.22f, 240f, 75f, 0.72f, noiseMix: 0.40f);
        sfxShieldPickup = CreateArpeggioClip("SFX_Shield", new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.09f, 0.24f, 0.58f);
        sfxHeartPickup = CreateArpeggioClip("SFX_Heart", new float[] { 587.33f, 880.00f }, 0.09f, 0.22f, 0.60f);
        sfxCorePickup = CreateArpeggioClip("SFX_Core", new float[] { 783.99f, 987.77f, 1174.66f, 1567.98f }, 0.065f, 0.20f, 0.58f);
        sfxGateOpen = CreateSweepClip("SFX_GateOpen", 0.42f, 130f, 290f, 0.60f, fadeAttack: 0.03f, harmonic: 0.45f);
        sfxCheckpoint = CreateArpeggioClip("SFX_Checkpoint", new float[] { 523.25f, 659.25f, 783.99f }, 0.08f, 0.24f, 0.56f);
        sfxTeleporter = CreateTeleporterClip("SFX_Teleporter", 0.38f);
        sfxSwitchPlate = CreateDoubleToneClip("SFX_SwitchPlate", 0.10f, 420f, 660f, 0.52f);
        sfxCrumble = CreateImpactClip("SFX_Crumble", 0.24f, 130f, 48f, 0.56f, noiseMix: 0.55f);
        sfxDeath = CreateArpeggioClip("SFX_Death", new float[] { 392.00f, 329.63f, 261.63f, 146.83f }, 0.11f, 0.28f, 0.62f);
        sfxLevelComplete = CreateArpeggioClip("SFX_LevelComplete", new float[] { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f }, 0.11f, 0.34f, 0.64f);
        sfxUIClick = CreateSweepClip("SFX_UIClick", 0.055f, 680f, 820f, 0.40f, fadeAttack: 0.004f, harmonic: 0.1f);
        sfxARPlace = CreateArpeggioClip("SFX_ARPlace", new float[] { 329.63f, 440.00f, 523.25f, 659.25f }, 0.075f, 0.26f, 0.58f);
    }

    static float WrapFreq16s(float hz)
    {
        // Snap frequency to integer cycles inside a 16-second loop so t=0 and t=16 connect seamlessly
        return Mathf.Round(hz * 16f) / 16f;
    }

    static AudioClip CreatePeacefulBgmClip()
    {
        const float duration = 16.0f;
        int count = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[count];

        // Peaceful Cmaj9 / Fmaj7 / Em7 / Am9 ambient pad progression + soft pentatonic bell notes
        // 4 chords x 4 seconds each = 16 seconds loop
        float[,] chords = new float[4, 4]
        {
            { 174.61f, 261.63f, 329.63f, 440.00f }, // Fmaj7 (F3, C4, E4, A4)
            { 196.00f, 293.66f, 329.63f, 493.88f }, // G6    (G3, D4, E4, B4)
            { 164.81f, 246.94f, 293.66f, 392.00f }, // Em7   (E3, B3, D4, G4)
            { 220.00f, 261.63f, 329.63f, 493.88f }  // Am9   (A3, C4, E4, B4)
        };

        // Pre-snap pad frequencies for zero-seam wraparound
        float[,] snapped = new float[4, 4];
        for (int c = 0; c < 4; c++)
            for (int n = 0; n < 4; n++)
                snapped[c, n] = WrapFreq16s(chords[c, n]);

        // 8 gentle spaced chime notes (startTimeSec, freqHz)
        float[] chimeTimes = { 0.5f, 2.5f, 4.5f, 6.5f, 8.5f, 10.5f, 12.5f, 14.3f };
        float[] chimeFreqs = { 523.25f, 659.25f, 587.33f, 783.99f, 493.88f, 659.25f, 523.25f, 440.00f };

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;

            // Crossfade across 4-second chord bars softly
            int chordIdx = Mathf.Min(3, (int)(t / 4f));
            int nextChord = (chordIdx + 1) % 4;
            float barLocal = t - chordIdx * 4f;
            float cross = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((barLocal - 3.0f) / 1.0f));

            float pad = 0f;
            for (int v = 0; v < 4; v++)
            {
                float s1 = Mathf.Sin(2f * Mathf.PI * snapped[chordIdx, v] * t);
                float s2 = Mathf.Sin(2f * Mathf.PI * snapped[nextChord, v] * t);
                pad += Mathf.Lerp(s1, s2, cross) * (v == 0 ? 0.30f : 0.16f);
            }
            // Gentle breathing shimmer LFO (1 cycle per 8 seconds = exact 2 cycles per 16s)
            float lfo = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.125f * t);
            float sample = pad * lfo * 0.42f;

            // Soft meditative chimes
            for (int c = 0; c < chimeTimes.Length; c++)
            {
                float dt = t - chimeTimes[c];
                if (dt >= 0f && dt < 1.5f)
                {
                    float env = Mathf.Min(1f, dt / 0.03f) * Mathf.Exp(-dt * 2.6f);
                    float f = chimeFreqs[c];
                    float bell = Mathf.Sin(2f * Mathf.PI * f * dt) + 0.18f * Mathf.Sin(2f * Mathf.PI * f * 2f * dt);
                    sample += bell * env * 0.13f;
                }
            }

            data[i] = Mathf.Clamp(sample, -0.92f, 0.92f);
        }

        var clip = AudioClip.Create("BGM_Peaceful", count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip CreateSweepClip(string name, float duration, float fStart, float fEnd, float gain, float fadeAttack, float harmonic)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] data = new float[count];
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float u = t / duration;
            float freq = Mathf.Lerp(fStart, fEnd, u);
            phase += 2f * Mathf.PI * freq / SampleRate;

            float atk = Mathf.Min(1f, t / Mathf.Max(0.002f, fadeAttack));
            float dec = 1f - u;
            dec = dec * dec;
            float sig = Mathf.Sin(phase) + harmonic * Mathf.Sin(phase * 2f);
            data[i] = Mathf.Clamp(sig * atk * dec * gain, -0.95f, 0.95f);
        }
        var clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip CreateImpactClip(string name, float duration, float fStart, float fEnd, float gain, float noiseMix)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] data = new float[count];
        float phase = 0f;
        uint seed = (uint)(name.GetHashCode() | 1);
        float lpNoise = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float u = t / duration;
            float freq = Mathf.Lerp(fStart, fEnd, Mathf.Sqrt(u));
            phase += 2f * Mathf.PI * freq / SampleRate;

            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            float white = ((seed & 0xFFFF) / 32768f) - 1f;
            lpNoise = Mathf.Lerp(lpNoise, white, 0.25f);

            float atk = Mathf.Min(1f, t / 0.004f);
            float dec = Mathf.Exp(-u * 4.5f) * (1f - u);
            float sig = Mathf.Sin(phase) * (1f - noiseMix) + lpNoise * noiseMix;
            data[i] = Mathf.Clamp(sig * atk * dec * gain, -0.95f, 0.95f);
        }
        var clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip CreateDoubleToneClip(string name, float duration, float f1, float f2, float gain)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float u = t / duration;
            float f = (u < 0.48f) ? f1 : f2;
            float localU = (u < 0.48f) ? (u / 0.48f) : ((u - 0.48f) / 0.52f);
            float env = Mathf.Min(1f, localU * 8f) * (1f - localU);
            data[i] = Mathf.Clamp(Mathf.Sin(2f * Mathf.PI * f * t) * env * gain, -0.95f, 0.95f);
        }
        var clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip CreateLavaClip(string name, float duration)
    {
        int count = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[count];
        uint seed = 0x51A7BEEF;
        float lp = 0f;
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float u = t / duration;
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            float white = ((seed & 0xFFFF) / 32768f) - 1f;
            lp = Mathf.Lerp(lp, white, 0.35f);
            float sizzle = white - lp * 0.7f;

            phase += 2f * Mathf.PI * Mathf.Lerp(150f, 65f, u) / SampleRate;
            float env = Mathf.Min(1f, t / 0.01f) * (1f - u);
            data[i] = Mathf.Clamp((Mathf.Sin(phase) * 0.45f + sizzle * 0.55f) * env * 0.68f, -0.95f, 0.95f);
        }
        var clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip CreateLaserHitClip(string name, float duration)
    {
        int count = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[count];
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float u = t / duration;
            float mod = Mathf.Sin(2f * Mathf.PI * 52f * t) * 160f;
            float freq = Mathf.Lerp(1100f, 210f, Mathf.Sqrt(u)) + mod;
            phase += 2f * Mathf.PI * freq / SampleRate;
            float wave = Mathf.Sin(phase) + 0.35f * Mathf.Sin(phase * 2.5f);
            float env = Mathf.Min(1f, t / 0.005f) * (1f - u) * (1f - u);
            data[i] = Mathf.Clamp(wave * env * 0.52f, -0.95f, 0.95f);
        }
        var clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip CreateSpikeSnapClip(string name, float duration)
    {
        int count = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[count];
        uint seed = 0x3A17C0DE;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float u = t / duration;
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            float white = ((seed & 0xFFFF) / 32768f) - 1f;
            float metal = Mathf.Sin(2f * Mathf.PI * 1380f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 2070f * t) * 0.35f;
            float env = Mathf.Min(1f, t / 0.003f) * Mathf.Exp(-u * 6f);
            data[i] = Mathf.Clamp((metal + white * 0.25f) * env * 0.55f, -0.95f, 0.95f);
        }
        var clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip CreateTeleporterClip(string name, float duration)
    {
        int count = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[count];
        float p1 = 0f, p2 = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float u = t / duration;
            float f1 = Mathf.Lerp(320f, 980f, u) + Mathf.Sin(2f * Mathf.PI * 18f * t) * 55f;
            float f2 = Mathf.Lerp(480f, 1460f, u);
            p1 += 2f * Mathf.PI * f1 / SampleRate;
            p2 += 2f * Mathf.PI * f2 / SampleRate;
            float env = Mathf.Sin(Mathf.PI * u);
            data[i] = Mathf.Clamp((Mathf.Sin(p1) * 0.55f + Mathf.Sin(p2) * 0.35f) * env * 0.55f, -0.95f, 0.95f);
        }
        var clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip CreateArpeggioClip(string name, float[] freqs, float noteStep, float noteDecay, float gain)
    {
        float total = freqs.Length * noteStep + noteDecay + 0.04f;
        int count = Mathf.RoundToInt(SampleRate * total);
        float[] data = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float sum = 0f;
            for (int n = 0; n < freqs.Length; n++)
            {
                float dt = t - n * noteStep;
                if (dt >= 0f && dt < noteDecay + 0.06f)
                {
                    float env = Mathf.Min(1f, dt / 0.008f) * Mathf.Exp(-dt / (noteDecay * 0.45f));
                    if (dt > noteDecay) env *= Mathf.Clamp01(1f - (dt - noteDecay) / 0.06f);
                    float f = freqs[n];
                    sum += (Mathf.Sin(2f * Mathf.PI * f * dt) + 0.2f * Mathf.Sin(2f * Mathf.PI * f * 2f * dt)) * env;
                }
            }
            data[i] = Mathf.Clamp(sum * gain * 0.45f, -0.95f, 0.95f);
        }

        var clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

