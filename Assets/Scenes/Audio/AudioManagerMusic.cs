using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-100)]
public class AudioManagerMusic : MonoBehaviour
{
    public static AudioManagerMusic I { get; private set; }

    [Header("Output Mixer")]
    public AudioMixerGroup musicOutput;

    [Header("Tracks")]
    public List<NamedTrack> tracks = new(); // riempi da Inspector

    [Serializable] public struct NamedTrack { public string id; public AudioClip clip; }

    Dictionary<string, AudioClip> _map = new();
    AudioSource _a, _b; // per crossfade
    Coroutine _xfade;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);

        _a = CreateSource(); _b = CreateSource();
        foreach (var t in tracks) if (t.clip && !string.IsNullOrEmpty(t.id)) _map[t.id] = t.clip;
    }

    AudioSource CreateSource()
    {
        var a = gameObject.AddComponent<AudioSource>();
        a.playOnAwake = false; a.loop = true; a.volume = 0f;
        a.outputAudioMixerGroup = musicOutput;
        return a;
    }

    public void Play(string id, float fade = 0.5f)
    {
        if (!_map.TryGetValue(id, out var clip) || clip == null) { Debug.LogWarning($"MUSIC '{id}' mancante."); return; }
        if (_xfade != null) StopCoroutine(_xfade);
        _xfade = StartCoroutine(CrossfadeTo(clip, fade));
    }

    IEnumerator CrossfadeTo(AudioClip next, float fade)
    {
        var from = _a.volume > _b.volume ? _a : _b;
        var to = from == _a ? _b : _a;

        to.clip = next; to.time = 0f; to.Play();

        float t = 0f; float inv = 1f / Mathf.Max(0.0001f, fade);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * inv;
            to.volume = Mathf.SmoothStep(0f, 1f, t);
            from.volume = Mathf.SmoothStep(1f, 0f, t);
            yield return null;
        }
        from.Stop(); from.volume = 0f; to.volume = 1f;
    }

    public void Stop(float fade = 0.3f)
    {
        if (_xfade != null) StopCoroutine(_xfade);
        StartCoroutine(FadeOutAll(fade));
    }

    IEnumerator FadeOutAll(float fade)
    {
        float t = 0f; float inv = 1f / Mathf.Max(0.0001f, fade);
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
