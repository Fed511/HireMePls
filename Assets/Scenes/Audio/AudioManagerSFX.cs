using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-200)] // parte prestissimo
public class AudioManagerSFX : MonoBehaviour
{
    public static AudioManagerSFX I { get; private set; }

    [Header("Output Mixer")]
    [Tooltip("Instrada tutti i SFX su questo AudioMixerGroup (es. 'SFX').")]
    public AudioMixerGroup sfxOutput;

    [Header("Clips registrati (ID → Clip)")]
    public List<NamedClip> clips = new();    // compila da Inspector

    [Header("Pool di AudioSource")]
    [Min(0)] public int initialSources = 8;
    [Min(1)] public int maxSources = 24;

    [Header("Opzioni")]
    [Tooltip("Stampa un warning se provi a suonare un ID non registrato.")]
    public bool logMissingIds = true;
    [Tooltip("Rendilo persistente tra le scene.")]
    public bool makeDontDestroyOnLoad = false;

    // ---- runtime ----
    readonly Dictionary<string, AudioClip> _clipById = new();
    readonly Dictionary<string, NamedClip> _settings = new();
    readonly List<AudioSource> _pool = new();
    int _next;

    [Serializable]
    public struct NamedClip
    {
        public string id;                       // es. "ui_click", "step_default"
        public AudioClip clip;
        [Range(0f, 1f)] public float baseVolume;
        [Range(0f, 1f)] public float volumeJitter;
        [Range(0f, 1f)] public float pitchJitter;
    }

    // ---------------- LIFECYCLE ----------------
    void Awake()
    {
        // Singleton + persistenza
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        if (makeDontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        RebuildMaps();            // registra ID → clip + impostazioni

        // Prewarm pool
        _pool.Clear();
        for (int i = 0; i < Mathf.Clamp(initialSources, 0, maxSources); i++)
            _pool.Add(CreateSource());
    }

    void OnDestroy()
    {
        if (I == this) I = null;
    }

    // Editor: ricostruisce la mappa quando tocchi l’inspector
    void OnValidate()
    {
        // evitiamo ricostruzioni a vuoto in Play
        if (!Application.isPlaying) RebuildMaps();
    }

    // ---------------- API PUBBLICA ----------------
    /// <summary>Suona l'ID registrato (volume/pitch opzionali)</summary>
    public void Play(string id, float volumeScale = 1f, float pitchScale = 1f)
    {
        if (!_clipById.TryGetValue(id, out var clip) || clip == null)
        {
            if (logMissingIds) Debug.LogWarning($"[AudioSFX] ID '{id}' non registrato o clip nullo.");
            return;
        }
        var a = GetSource();
        ForceOutput(a);
        ApplyJitter(a, GetSettings(id));
        a.volume *= Mathf.Clamp01(volumeScale);
        a.pitch *= Mathf.Max(0.01f, pitchScale);
        a.spatialBlend = 0f; // 2D
        a.PlayOneShot(clip, a.volume);
    }

    /// <summary>Suona l'ID a una posizione 3D (one-shot)</summary>
    public void PlayAt(string id, Vector3 worldPos, float volumeScale = 1f, float pitchScale = 1f)
    {
        if (!_clipById.TryGetValue(id, out var clip) || clip == null)
        {
            if (logMissingIds) Debug.LogWarning($"[AudioSFX] ID '{id}' non registrato o clip nullo.");
            return;
        }
        var a = GetSource();
        ForceOutput(a);
        ApplyJitter(a, GetSettings(id));
        a.volume *= Mathf.Clamp01(volumeScale);
        a.pitch *= Mathf.Max(0.01f, pitchScale);
        a.transform.position = worldPos;
        a.spatialBlend = 1f; // 3D
        a.PlayOneShot(clip, a.volume);
        a.spatialBlend = 0f; // reset per UI
    }

    /// <summary>Stoppa tutti gli AudioSource del pool (non interrompe OneShot già partiti su altre sorgenti esterne).</summary>
    public void StopAll()
    {
        for (int i = 0; i < _pool.Count; i++)
            if (_pool[i]) _pool[i].Stop();
    }

    /// <summary>Quanti ID sono registrati (debug/diagnostica).</summary>
    public int RegisteredIdCount() => _clipById.Count;

    // ---------------- IMPLEMENTAZIONE ----------------
    void RebuildMaps()
    {
        _clipById.Clear();
        _settings.Clear();

        for (int i = 0; i < clips.Count; i++)
        {
            var nc = clips[i];
            if (nc.clip == null || string.IsNullOrWhiteSpace(nc.id)) continue;

            // se ci sono duplicati, l'ultimo in lista vince (lo segnaliamo in editor)
#if UNITY_EDITOR
            if (_clipById.ContainsKey(nc.id))
                Debug.LogWarning($"[AudioSFX] ID duplicato '{nc.id}' → userò l'ultimo in lista.");
#endif
            _clipById[nc.id] = nc.clip;
            _settings[nc.id] = nc;
        }
    }

    AudioSource CreateSource()
    {
        var a = gameObject.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = false;
        a.spatialBlend = 0f;
        a.rolloffMode = AudioRolloffMode.Linear;
        a.minDistance = 1f;
        a.maxDistance = 25f;
        ForceOutput(a);
        return a;
    }

    void ForceOutput(AudioSource a)
    {
        // instrada SEMPRE al gruppo corretto del Mixer (evita sorprese su duplicazioni/prefab)
        if (sfxOutput != null && a.outputAudioMixerGroup != sfxOutput)
            a.outputAudioMixerGroup = sfxOutput;
    }

    AudioSource GetSource()
    {
        // round-robin: prendi il primo libero, altrimenti crea fino a maxSources,
        // infine riusa quello puntato da _next
        for (int i = 0; i < _pool.Count; i++)
        {
            _next = (_next + 1) % _pool.Count;
            var a = _pool[_next];
            if (!a || !a.isActiveAndEnabled) continue;
            if (!a.isPlaying) return a;
        }

        if (_pool.Count < Mathf.Max(1, maxSources))
        {
            var a = CreateSource();
            _pool.Add(a);
            _next = _pool.Count - 1;
            return a;
        }

        return _pool.Count > 0 ? _pool[_next] : CreateSource();
    }

    (float volBase, float volJ, float pitchJ) GetSettings(string id)
    {
        if (_settings.TryGetValue(id, out var s))
        {
            float v = s.baseVolume <= 0f ? 1f : s.baseVolume;
            return (Mathf.Max(0.0001f, v), s.volumeJitter, s.pitchJitter);
        }
        return (1f, 0f, 0f);
    }

    void ApplyJitter(AudioSource a, (float v, float vJ, float pJ) s)
    {
        a.volume = s.v * (1f + UnityEngine.Random.Range(-s.vJ, s.vJ));
        a.pitch = 1f * (1f + UnityEngine.Random.Range(-s.pJ, s.pJ));
    }
}
