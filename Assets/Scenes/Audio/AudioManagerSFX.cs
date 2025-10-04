using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-200)] // inizializza presto
public class AudioManagerSFX : MonoBehaviour
{
    public static AudioManagerSFX I { get; private set; }

    [Header("Output Mixer")]
    public AudioMixerGroup sfxOutput;

    [Header("Clips (ID → Clip + impostazioni)")]
    public List<NamedClip> clips = new();

    [Header("Pool")]
    [Min(0)] public int initialSources = 8;
    [Min(1)] public int maxSources = 24;

    [Header("Opzioni")]
    public bool logMissingIds = true;

    [Serializable]
    public struct NamedClip
    {
        public string id;                    // es. "ui_click", "footstep"
        public AudioClip clip;
        [Range(0f, 1f)] public float baseVolume;
        [Range(0f, 1f)] public float volumeJitter;
        [Range(0f, 1f)] public float pitchJitter;
    }

    // ---- runtime ----
    readonly Dictionary<string, AudioClip> _map = new(); // ID → Clip
    readonly List<AudioSource> _pool = new();
    int _next;

    // -------------- LIFECYCLE --------------
    void Awake()
    {
        // singleton + no duplicati
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;

        // ⚠️ DDOL solo su root: stacca dal parent se serve e rendi persistente il blocco
        if (transform.root != transform) transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);

        RebuildMap();

        _pool.Clear();
        for (int i = 0; i < Mathf.Clamp(initialSources, 0, maxSources); i++)
            _pool.Add(CreateSource());
    }

    void OnDestroy() { if (I == this) I = null; }

    void OnValidate()
    {
        // ricostruisci la mappa anche in Editor per avere gli ID aggiornati
        RebuildMap();
    }

    // -------------- API PUBBLICA --------------
    /// <summary>Suona un ID registrato (2D). volumeScale/pitchScale opzionali.</summary>
    public void Play(string id, float volumeScale = 1f, float pitchScale = 1f)
    {
        if (!_map.TryGetValue(id, out var clip) || clip == null)
        {
            if (logMissingIds) Debug.LogWarning($"[SFX] ID '{id}' mancante o clip nullo.");
            return;
        }

        var a = GetSource();
        EnsureOutput(a);

        // leggi i parametri LIVE dalla lista (così cambiano subito da Inspector)
        var set = GetSettingsLive(id);

        ApplyJitter(a, set);
        a.volume *= Mathf.Clamp01(volumeScale);
        a.pitch *= Mathf.Max(0.01f, pitchScale);
        a.spatialBlend = 0f; // 2D
        a.PlayOneShot(clip, a.volume);
    }

    /// <summary>Suona un ID in una posizione 3D (one-shot).</summary>
    public void PlayAt(string id, Vector3 worldPos, float volumeScale = 1f, float pitchScale = 1f)
    {
        if (!_map.TryGetValue(id, out var clip) || clip == null)
        {
            if (logMissingIds) Debug.LogWarning($"[SFX] ID '{id}' mancante o clip nullo.");
            return;
        }

        var a = GetSource();
        EnsureOutput(a);

        var set = GetSettingsLive(id);

        ApplyJitter(a, set);
        a.volume *= Mathf.Clamp01(volumeScale);
        a.pitch *= Mathf.Max(0.01f, pitchScale);
        a.transform.position = worldPos;
        a.spatialBlend = 1f; // 3D
        a.PlayOneShot(clip, a.volume);
        a.spatialBlend = 0f; // reset per UI
    }

    /// <summary>Ferma tutti i source del pool.</summary>
    public void StopAll()
    {
        for (int i = 0; i < _pool.Count; i++)
            if (_pool[i]) _pool[i].Stop();
    }

    public int RegisteredIdCount() => _map.Count;

    // -------------- IMPLEMENTAZIONE --------------
    void RebuildMap()
    {
        _map.Clear();
        for (int i = 0; i < clips.Count; i++)
        {
            var c = clips[i];
            if (c.clip == null || string.IsNullOrWhiteSpace(c.id)) continue;
#if UNITY_EDITOR
            if (_map.ContainsKey(c.id))
                Debug.LogWarning($"[SFX] ID duplicato '{c.id}' → userò l'ultimo in lista.");
#endif
            _map[c.id] = c.clip;
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
        EnsureOutput(a);
        return a;
    }

    void EnsureOutput(AudioSource a)
    {
        if (sfxOutput != null && a.outputAudioMixerGroup != sfxOutput)
            a.outputAudioMixerGroup = sfxOutput;
    }

    AudioSource GetSource()
    {
        // round-robin: prendi il primo libero; crea fino a max; altrimenti riusa il corrente
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

    (float v, float vJ, float pJ) GetSettingsLive(string id)
    {
        // legge dalla lista (live) per riflettere subito i cambi in Inspector
        int idx = clips.FindIndex(c => c.id == id && c.clip);
        if (idx >= 0)
        {
            var s = clips[idx];
            float baseV = s.baseVolume <= 0f ? 1f : s.baseVolume;
            return (Mathf.Max(0.0001f, baseV), s.volumeJitter, s.pitchJitter);
        }
        // default sicuro
        return (1f, 0f, 0f);
    }

    void ApplyJitter(AudioSource a, (float v, float vJ, float pJ) s)
    {
        a.volume = s.v * (1f + UnityEngine.Random.Range(-s.vJ, s.vJ));
        a.pitch = 1f * (1f + UnityEngine.Random.Range(-s.pJ, s.pJ));
    }
}
