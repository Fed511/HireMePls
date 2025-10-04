using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-200)] // parte prestissimo
public class AudioManagerMusic : MonoBehaviour
{
    public static AudioManagerMusic I { get; private set; }

    [Header("Output Mixer")]
    public AudioMixerGroup musicOutput;

    [Header("Tracks (ID → Clip)")]
    public List<NamedTrack> tracks = new();
    [System.Serializable] public struct NamedTrack { public string id; public AudioClip clip; }

    [Header("Autoplay opzionale")]
    public string startupTrackId = "";   // es. "menu" nella prima scena
    public float startupFade = 0.6f;

    readonly Dictionary<string, AudioClip> _map = new();
    AudioSource _a, _b;  // crossfade A/B
    Coroutine _xfade;

    void Awake()
    {
        // Singleton + persistenza
        if (I && I != this) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);

        RebuildMap();
        _a = CreateSource(); _b = CreateSource();

        if (!string.IsNullOrEmpty(startupTrackId))
            Play(startupTrackId, startupFade);
    }

    void OnDestroy() { if (I == this) I = null; }

    void OnValidate() { RebuildMap(); }

    void RebuildMap()
    {
        _map.Clear();
        foreach (var t in tracks)
            if (t.clip && !string.IsNullOrWhiteSpace(t.id)) _map[t.id] = t.clip;
    }

    AudioSource CreateSource()
    {
        var a = gameObject.AddComponent<AudioSource>();
        a.playOnAwake = false; a.loop = true; a.volume = 0f; a.spatialBlend = 0f;
        if (musicOutput) a.outputAudioMixerGroup = musicOutput;
        return a;
    }

    public void Play(string id, float fade = 0.5f)
    {
        if (!_map.TryGetValue(id, out var clip) || clip == null) { Debug.LogWarning($"[Music] '{id}' mancante."); return; }
        if (_xfade != null) StopCoroutine(_xfade);
        _xfade = StartCoroutine(CrossfadeTo(clip, Mathf.Max(0f, fade)));
    }

    public void PlayIfDifferent(string id, float fade = 0.5f)
    {
        if (!_map.TryGetValue(id, out var clip) || clip == null) { Debug.LogWarning($"[Music] '{id}' mancante."); return; }
        var current = _a.volume > _b.volume ? _a.clip : _b.clip;
        if (current == clip) return;
        Play(id, fade);
    }

    public void Stop(float fade = 0.3f)
    {
        if (_xfade != null) StopCoroutine(_xfade);
        StartCoroutine(FadeOutAll(Mathf.Max(0f, fade)));
    }

    IEnumerator CrossfadeTo(AudioClip next, float fade)
    {
        var from = _a.volume > _b.volume ? _a : _b;
        var to = from == _a ? _b : _a;

        to.clip = next; to.time = 0f; to.Play();

        if (fade <= 0f) { from.Stop(); from.volume = 0f; to.volume = 1f; yield break; }

        float t = 0f, inv = 1f / fade;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * inv;
            to.volume = Mathf.SmoothStep(0f, 1f, t);
            from.volume = Mathf.SmoothStep(1f, 0f, t);
            yield return null;
        }
        from.Stop(); from.volume = 0f; to.volume = 1f;
    }

    IEnumerator FadeOutAll(float fade)
    {
        float t = 0f, inv = 1f / Mathf.Max(0.0001f, fade);
        float a0 = _a.volume, b0 = _b.volume;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * inv;
            _a.volume = Mathf.Lerp(a0, 0f, t);
            _b.volume = Mathf.Lerp(b0, 0f, t);
            yield return null;
        }
        _a.Stop(); _b.Stop(); _a.volume = _b.volume = 0f;
    }
}
