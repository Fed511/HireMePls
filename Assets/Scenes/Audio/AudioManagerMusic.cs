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

    // --- controlli utente (modificabili da Inspector o via API) ---
    [Header("User Controls (globali)")]
    [Range(0f, 1f)] public float userVolume = 1f;   // moltiplicatore finale
    [Range(0.1f, 3f)] public float userPitch = 1f;
    [Range(-1f, 1f)] public float userPan = 0f;

    // runtime
    readonly Dictionary<string, AudioClip> _map = new();
    AudioSource _a, _b;                 // doppia sorgente per crossfade
    Coroutine _xfade;                   // crossfade tracce
    Coroutine _xfadeVolume;             // ramp di userVolume

    // -------- LIFECYCLE --------
    void Awake()
    {
        // Singleton + persistenza (assicurati di essere root prima di DDOL)
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;

        if (transform.root != transform)
            transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);

        RebuildMap();
        _a = CreateSource();
        _b = CreateSource();

        if (!string.IsNullOrEmpty(startupTrackId))
            Play(startupTrackId, startupFade);
    }

    void OnDestroy() { if (I == this) I = null; }

    void OnValidate() { RebuildMap(); }

    // -------- SETUP / UTILS --------
    void RebuildMap()
    {
        _map.Clear();
        for (int i = 0; i < tracks.Count; i++)
        {
            var t = tracks[i];
            if (t.clip && !string.IsNullOrWhiteSpace(t.id))
                _map[t.id] = t.clip;
        }
    }

    AudioSource CreateSource()
    {
        var a = gameObject.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = true;
        a.volume = 0f;
        a.spatialBlend = 0f;
        if (musicOutput) a.outputAudioMixerGroup = musicOutput;
        ApplyUserParamsTo(a);
        return a;
    }

    AudioSource Active() => _a.volume > _b.volume ? _a : _b;
    AudioSource Inactive() => Active() == _a ? _b : _a;

    void ApplyUserParamsTo(AudioSource s)
    {
        if (!s) return;
        s.pitch = userPitch;
        s.panStereo = userPan;
    }

    void ResnapVolumesToUserMul()
    {
        // Mantiene il rapporto A/B e applica il moltiplicatore globale
        float sum = _a.volume + _b.volume;
        if (sum <= 0f) return;
        float ra = _a.volume / sum;
        float rb = _b.volume / sum;
        _a.volume = ra * userVolume;
        _b.volume = rb * userVolume;
    }

    void StopCoroutineSafe(ref Coroutine c)
    {
        if (c != null) { StopCoroutine(c); c = null; }
    }

    // -------- API PRINCIPALI --------
    public void Play(string id, float fade = 0.5f)
    {
        if (!_map.TryGetValue(id, out var clip) || clip == null)
        {
            Debug.LogWarning($"[Music] '{id}' mancante.");
            return;
        }
        StopCoroutineSafe(ref _xfade);
        _xfade = StartCoroutine(CrossfadeTo(clip, Mathf.Max(0f, fade)));
    }

    public void PlayIfDifferent(string id, float fade = 0.5f)
    {
        if (!_map.TryGetValue(id, out var clip) || clip == null)
        {
            Debug.LogWarning($"[Music] '{id}' mancante.");
            return;
        }
        var current = Active().clip;
        if (current == clip) return;
        Play(id, fade);
    }

    public void PlayImmediate(string id)
    {
        if (!_map.TryGetValue(id, out var clip) || clip == null)
        {
            Debug.LogWarning($"[Music] '{id}' mancante.");
            return;
        }

        var from = Active();
        var to = Inactive();

        to.clip = clip;
        to.time = 0f;
        ApplyUserParamsTo(to);
        to.volume = 1f * userVolume;
        to.Play();

        if (from.isPlaying) { from.Stop(); from.volume = 0f; }
    }

    public void PlayWithStart(string id, float fade, float startSeconds, bool randomizeStart)
    {
        if (!_map.TryGetValue(id, out var clip) || clip == null)
        {
            Debug.LogWarning($"[Music] '{id}' mancante.");
            return;
        }
        StopCoroutineSafe(ref _xfade);
        _xfade = StartCoroutine(CrossfadeToWithStart(clip, Mathf.Max(0f, fade), startSeconds, randomizeStart));
    }

    public void Stop(float fade = 0.3f)
    {
        StopCoroutineSafe(ref _xfade);
        StartCoroutine(FadeOutAll(Mathf.Max(0f, fade)));
    }

    // -------- CONTROLLI "LIVE" (da Inspector o script) --------
    public void SetUserVolume(float target, float ramp = 0f) // 0..1
    {
        target = Mathf.Clamp01(target);
        if (Mathf.Approximately(target, userVolume)) { userVolume = target; return; }
        StopCoroutineSafe(ref _xfadeVolume);
        _xfadeVolume = StartCoroutine(RampUserVolume(target, ramp));
    }

    IEnumerator RampUserVolume(float target, float ramp)
    {
        float start = userVolume;
        if (ramp <= 0f) { userVolume = target; ResnapVolumesToUserMul(); yield break; }
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / ramp;
            userVolume = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            ResnapVolumesToUserMul();
            yield return null;
        }
        userVolume = target;
        ResnapVolumesToUserMul();
    }

    public void SetUserPitch(float pitch, float ramp = 0f)
    {
        pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        if (ramp <= 0f)
        {
            userPitch = pitch;
            ApplyUserParamsTo(_a);
            ApplyUserParamsTo(_b);
            return;
        }
        StartCoroutine(RampPitch(pitch, ramp));
    }

    IEnumerator RampPitch(float target, float ramp)
    {
        float startA = _a ? _a.pitch : 1f;
        float startB = _b ? _b.pitch : 1f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / ramp;
            float p = Mathf.Lerp(startA, target, t);
            if (_a) _a.pitch = p;
            float q = Mathf.Lerp(startB, target, t);
            if (_b) _b.pitch = q;
            yield return null;
        }
        userPitch = target;
    }

    public void SetUserPan(float pan, float ramp = 0f) // -1..1
    {
        pan = Mathf.Clamp(pan, -1f, 1f);
        if (ramp <= 0f)
        {
            userPan = pan;
            ApplyUserParamsTo(_a);
            ApplyUserParamsTo(_b);
            return;
        }
        StartCoroutine(RampPan(pan, ramp));
    }

    IEnumerator RampPan(float target, float ramp)
    {
        float startA = _a ? _a.panStereo : 0f;
        float startB = _b ? _b.panStereo : 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / ramp;
            float p = Mathf.Lerp(startA, target, t);
            if (_a) _a.panStereo = p;
            float q = Mathf.Lerp(startB, target, t);
            if (_b) _b.panStereo = q;
            yield return null;
        }
        userPan = target;
    }

    // -------- COROUTINE --------
    IEnumerator CrossfadeTo(AudioClip next, float fade)
    {
        var from = Active();
        var to = Inactive();

        to.clip = next;
        to.time = 0f;
        ApplyUserParamsTo(to);
        to.Play();

        if (fade <= 0f)
        {
            from.Stop(); from.volume = 0f;
            to.volume = 1f * userVolume;
            yield break;
        }

        float t = 0f, inv = 1f / fade;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * inv;
            to.volume = Mathf.SmoothStep(0f, 1f, t) * userVolume;
            from.volume = Mathf.SmoothStep(1f, 0f, t) * userVolume;
            yield return null;
        }
        from.Stop(); from.volume = 0f; to.volume = 1f * userVolume;
    }

    IEnumerator CrossfadeToWithStart(AudioClip next, float fade, float startSeconds, bool randomizeStart)
    {
        var from = Active();
        var to = Inactive();

        to.clip = next;
        float len = next.length;
        float start = randomizeStart
            ? Random.Range(0f, Mathf.Max(0f, len - 0.05f))
            : Mathf.Clamp(startSeconds, 0f, Mathf.Max(0f, len - 0.05f));
        to.time = start;
        ApplyUserParamsTo(to);
        to.Play();

        if (fade <= 0f)
        {
            from.Stop(); from.volume = 0f;
            to.volume = 1f * userVolume;
            yield break;
        }

        float t = 0f, inv = 1f / fade;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * inv;
            to.volume = Mathf.SmoothStep(0f, 1f, t) * userVolume;
            from.volume = Mathf.SmoothStep(1f, 0f, t) * userVolume;
            yield return null;
        }
        from.Stop(); from.volume = 0f; to.volume = 1f * userVolume;
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
        _a.Stop(); _b.Stop();
        _a.volume = _b.volume = 0f;
    }
}
