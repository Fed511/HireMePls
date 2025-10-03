using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManagerSFX : MonoBehaviour
{
    public static AudioManagerSFX I { get; private set; }

    [Header("Output Mixer")]
    public AudioMixerGroup sfxOutput;

    [Header("Clips")]
    public List<NamedClip> clips = new(); // riempi da Inspector

    [Header("Pool")]
    public int initialSources = 8;
    public int maxSources = 24;

    readonly Dictionary<string, AudioClip> _map = new();
    readonly List<AudioSource> _pool = new();
    int _next;

    [Serializable]
    public struct NamedClip
    {
        public string id;          // es. "ui_hover", "ui_click"
        public AudioClip clip;
        [Range(0f, 1f)] public float baseVolume;
        [Range(0f, 1f)] public float volumeJitter;
        [Range(0f, 1f)] public float pitchJitter;
    }

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);

        foreach (var nc in clips) if (nc.clip && !string.IsNullOrEmpty(nc.id)) _map[nc.id] = nc.clip;

        for (int i = 0; i < initialSources; i++) _pool.Add(CreateSource());
    }

    AudioSource CreateSource()
    {
        var a = gameObject.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = false;
        a.outputAudioMixerGroup = sfxOutput;
        return a;
    }

    AudioSource GetSource()
    {
        // round-robin; crea se serve
        for (int i = 0; i < _pool.Count; i++)
        {
            _next = (_next + 1) % _pool.Count;
            if (!_pool[_next].isPlaying) return _pool[_next];
        }
        if (_pool.Count < maxSources)
        {
            var a = CreateSource();
            _pool.Add(a);
            return a;
        }
        return _pool[_next]; // sovrascrive il più “vecchio”
    }

    public void Play(string id)
    {
        if (!_map.TryGetValue(id, out var clip) || clip == null) { Debug.LogWarning($"SFX '{id}' mancante."); return; }
        Play(clip, GetSettings(id));
    }

    public void PlayAt(string id, Vector3 pos)
    {
        if (!_map.TryGetValue(id, out var clip) || clip == null) return;
        var a = GetSource();
        a.transform.position = pos;
        ApplyJitter(a, GetSettings(id));
        a.spatialBlend = 1f; // 3D
        a.PlayOneShot(clip, a.volume);
        a.spatialBlend = 0f; // reset per UI
    }

    (float volBase, float volJ, float pitchJ) GetSettings(string id)
    {
        var nc = clips.Find(c => c.id == id);
        return (Mathf.Max(0.001f, nc.baseVolume == 0 ? 1f : nc.baseVolume), nc.volumeJitter, nc.pitchJitter);
    }

    void ApplyJitter(AudioSource a, (float v, float vJ, float pJ) s)
    {
        a.volume = s.v * (1f + UnityEngine.Random.Range(-s.vJ, s.vJ));
        a.pitch = 1f * (1f + UnityEngine.Random.Range(-s.pJ, s.pJ));
    }

    void Play(AudioClip clip, (float v, float vJ, float pJ) s)
    {
        var a = GetSource();
        ApplyJitter(a, s);
        a.spatialBlend = 0f; // UI 2D
        a.PlayOneShot(clip, a.volume);
    }
}
