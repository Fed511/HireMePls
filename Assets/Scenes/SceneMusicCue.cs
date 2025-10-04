using System.Collections;
using UnityEngine;

public class SceneMusicCue : MonoBehaviour
{
    public enum TriggerMoment { OnStart, OnEnable, Manual }
    public enum CueMode { Play, PlayIfDifferent, PlayImmediate, Stop }

    [Header("Track")]
    public string trackId = "startOST";
    public CueMode mode = CueMode.PlayIfDifferent;

    [Header("Timing")]
    public TriggerMoment trigger = TriggerMoment.OnStart;
    public float delay = 0f;
    public bool useUnscaledTime = true;
    public float fade = 0.3f;

    [Header("Start Offset")]
    public bool useStartOffset = false;
    public float startSeconds = 0f;
    public bool randomizeStart = false;

    [Header("Mix Controls (applicati dopo la cue)")]
    public bool setVolume = false;
    [Range(0f, 1f)] public float volumeMul = 1f;
    public float volumeRamp = 0f;

    public bool setPitch = false;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public float pitchRamp = 0f;

    public bool setPan = false;
    [Range(-1f, 1f)] public float pan = 0f;
    public float panRamp = 0f;

    [Header("Misc")]
    public bool stopOnDisable = false;
    public bool debugLog = false;

    Coroutine _pending;

    void OnEnable()
    {
        if (trigger == TriggerMoment.OnEnable) StartCue();
    }

    void Start()
    {
        if (trigger == TriggerMoment.OnStart) StartCue();
    }

    void OnDisable()
    {
        if (_pending != null) { StopCoroutine(_pending); _pending = null; }
        if (stopOnDisable) AudioManagerMusic.I?.Stop(fade);
    }

    public void StartCue()
    {
        if (_pending != null) StopCoroutine(_pending);
        _pending = StartCoroutine(RunCue());
    }

    IEnumerator RunCue()
    {
        if (delay > 0f)
        {
            if (useUnscaledTime)
            {
                float t = 0f; while (t < delay) { t += Time.unscaledDeltaTime; yield return null; }
            }
            else yield return new WaitForSeconds(delay);
        }

        var mus = AudioManagerMusic.I;
        if (mus == null) yield break;

        // 1) PLAY / STOP
        if (mode == CueMode.Stop)
        {
            if (debugLog) Debug.Log($"[SceneMusicCue] Stop (fade={fade:F2})");
            mus.Stop(fade);
        }
        else
        {
            if (useStartOffset || randomizeStart)
            {
                if (debugLog) Debug.Log($"[SceneMusicCue] PlayWithStart '{trackId}' (fade={fade:F2}, start={startSeconds:F2}, rnd={randomizeStart})");
                mus.PlayWithStart(trackId, fade, startSeconds, randomizeStart);
            }
            else
            {
                switch (mode)
                {
                    case CueMode.Play:
                        if (debugLog) Debug.Log($"[SceneMusicCue] Play '{trackId}' (fade={fade:F2})");
                        mus.Play(trackId, fade);
                        break;
                    case CueMode.PlayIfDifferent:
                        if (debugLog) Debug.Log($"[SceneMusicCue] PlayIfDifferent '{trackId}' (fade={fade:F2})");
                        mus.PlayIfDifferent(trackId, fade);
                        break;
                    case CueMode.PlayImmediate:
                        if (debugLog) Debug.Log($"[SceneMusicCue] PlayImmediate '{trackId}'");
                        mus.PlayImmediate(trackId);
                        break;
                }
            }
        }

        // 2) MIX CONTROLS (indipendenti dalla cue)
        if (setVolume) mus.SetUserVolume(volumeMul, volumeRamp);
        if (setPitch) mus.SetUserPitch(pitch, pitchRamp);
        if (setPan) mus.SetUserPan(pan, panRamp);

        _pending = null;
    }
}
