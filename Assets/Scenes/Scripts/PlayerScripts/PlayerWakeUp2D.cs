using System.Collections;
using UnityEngine;
using UnityEngine.U2D;        // PixelPerfectCamera (se presente)
using Unity.Cinemachine;      // ✅ CM3

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerWakeUp2D : MonoBehaviour
{
    // --------- STATE PUBBLICO (per SpawnOnSceneLoad & co.) ---------
    public static bool IsRunning { get; private set; }
    public static bool HasPlayedOnce => _hasPlayedOnce;

    [Header("Camera Shake all'arrivo")]
    public bool shakeOnStand = true;
    [Tooltip("Ampiezza in unità di OrthoSize (0.06 ≈ 6% della size).")]
    public float shakeAmplitude = 0.06f;
    [Tooltip("Durata totale dello shake.")]
    public float shakeDuration = 0.22f;
    [Tooltip("Quante oscillazioni complete durante lo shake.")]
    public int shakeCycles = 4;
    [Tooltip("Curva d'inviluppo ampiezza (0→1 nel tempo).")]
    public AnimationCurve shakeEnvelope = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Target grafico (child)")]
    public Transform visual;
    public bool autoFindChildSprite = true;

    [Header("Timing")]
    public bool runOnStart = true;
    public bool onlyFirstTime = true;
    public float preDelay = 0.25f;
    public float riseDuration = 1.2f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Posa iniziale/finale")]
    public float layAngleDeg = -90f;
    public Vector2 layLocalOffset = new Vector2(0f, -0.1f);
    public Vector2 standLocalOffset = Vector2.zero;

    [Header("Controlli da (dis)attivare")]
    public Behaviour[] inputSourcesToDisable;
    public Behaviour movementMotorToDisable;    // es. MovementMotor2D
    public Behaviour[] optionalAnimsToDisable;  // es. WobbleAnimator2D
    public Behaviour[] optionalAnimsToEnable;

    [Header("Audio (duck + SFX)")]
    public bool duckMusic = true;
    [Range(0f, 1f)] public float duckedVolume = 0.65f;
    public float duckInTime = 0.2f;
    public float duckOutTime = 0.6f;
    public string wakeSfxId = "";

    [Tooltip("Ferma tutti i SFX attivi alla fine della cutscene.")]
    public bool stopAllSfxOnEnd = true;

    [Header("Camera (Cinemachine 3)")]
    [Tooltip("Assegna la tua CinemachineCamera (CM3). Se vuoto, ne cerca una in scena.")]
    public CinemachineCamera cineCam;                 // ✅ CM3
    public bool useCameraZoom = true;

    [Tooltip("Quanto partire più LONTANO della size base (0.8 = molto fuori).")]
    public float startZoomOutAmount = 0.8f;           // >0 = zoom-out (ingrandisce OrthoSize)
    [Tooltip("Quanto dura tutto lo zoom-in fino alla size base.")]
    public float zoomInTotalTime = 2.0f;

    [Tooltip("Se presente una PixelPerfectCamera sulla Main Camera, spegnila durante lo zoom per evitare il clamp.")]
    public bool disablePixelPerfectDuringZoom = true;

    [Header("Stabilità extra")]
    public bool forceZeroGravity = true;
    public bool keepZeroGravityAfter = true; // top-down consigliato
    public float inputDeadAfter = 0.15f;

    [Header("Skip")]
    public bool allowSkip = true;
    public KeyCode skipKey = KeyCode.Space;

    static bool _hasPlayedOnce;

    // runtime cache
    Rigidbody2D _rb;
    RigidbodyType2D _origBodyType;
    float _origGravity;
    Quaternion _rotLay, _rotStand;
    Vector3 _posLay, _posStand, _baseLocal;
    float _baseOrtho;                 // Ortho base della vcam (Lens.OrthographicSize)
    PixelPerfectCamera _ppc;          // se presente su Main Camera

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _origBodyType = _rb.bodyType;
        _origGravity = _rb.gravityScale;

        if (!visual && autoFindChildSprite)
        {
            var sr = GetComponentInChildren<SpriteRenderer>(true);
            if (sr) visual = sr.transform;
        }
        if (!visual) { Debug.LogWarning($"{name}: PlayerWakeUp2D senza 'visual'."); enabled = false; return; }

        _rotLay = Quaternion.Euler(0, 0, layAngleDeg);
        _rotStand = Quaternion.identity;
        _posLay = layLocalOffset;
        _posStand = standLocalOffset;

        // 🎥 Cinemachine 3
        if (!cineCam) cineCam = Object.FindFirstObjectByType<CinemachineCamera>(FindObjectsInactive.Exclude);
        if (!cineCam)
        {
            Debug.LogWarning($"{name}: nessuna CinemachineCamera trovata. Lo zoom sarà ignorato.");
            useCameraZoom = false;
        }
        else
        {
            _baseOrtho = cineCam.Lens.OrthographicSize;
        }

        var cam = Camera.main;
        if (cam) _ppc = cam.GetComponent<PixelPerfectCamera>();
    }

    void Start()
    {
        if (!runOnStart) return;
        if (onlyFirstTime && _hasPlayedOnce) { enabled = false; return; }
        StartCoroutine(RunCutscene());
    }

    IEnumerator RunCutscene()
    {
        IsRunning = true;
        ControlLock.Push("PlayerWakeUp2D");

        // stop controlli/animazioni
        Toggle(inputSourcesToDisable, false);
        Toggle(optionalAnimsToDisable, false);
        if (movementMotorToDisable) movementMotorToDisable.enabled = false;

        // isola fisica
        _rb.bodyType = RigidbodyType2D.Kinematic;
#if UNITY_600_OR_NEWER
        _rb.linearVelocity = Vector2.zero;
#else
        _rb.linearVelocity = Vector2.zero;
#endif
        _rb.angularVelocity = 0;
        if (forceZeroGravity) _rb.gravityScale = 0f;

        // posa sdraiata (solo grafica)
        _baseLocal = visual.localPosition;
        visual.localRotation = _rotLay;
        visual.localPosition = _baseLocal + (Vector3)_posLay;

        // duck musica
        if (duckMusic && AudioManagerMusic.I != null)
            AudioManagerMusic.I.SetUserVolume(duckedVolume, duckInTime);

        // Zoom-out immediato e zoom-in lento alla base
        if (useCameraZoom && cineCam)
        {
            float startOut = _baseOrtho + Mathf.Max(0f, startZoomOutAmount);
            StartCoroutine(CMZoomRoutine(startOut, _baseOrtho, zoomInTotalTime, presetAtStart: true, keepPpcDisabled: true));
        }

        // pre-delay (skippabile)
        if (preDelay > 0f)
        {
            float t0 = Time.time;
            while (Time.time - t0 < preDelay)
            {
                if (allowSkip && Input.GetKeyDown(skipKey)) break;
                yield return null;
            }
        }

        // SFX opzionale
        if (!string.IsNullOrEmpty(wakeSfxId))
            AudioManagerSFX.I?.Play(wakeSfxId);

        // anim alzata (in parallelo allo zoom-in)
        float t = 0f;
        while (t < riseDuration)
        {
            if (allowSkip && Input.GetKeyDown(skipKey)) break;
            t += Time.deltaTime;
            float e = ease.Evaluate(Mathf.Clamp01(t / Mathf.Max(0.0001f, riseDuration)));
            visual.localRotation = Quaternion.SlerpUnclamped(_rotLay, _rotStand, e);
            visual.localPosition = Vector3.LerpUnclamped(_baseLocal + (Vector3)_posLay,
                                                         _baseLocal + (Vector3)_posStand, e);
            yield return null;
        }
        visual.localRotation = _rotStand;
        visual.localPosition = _baseLocal + (Vector3)_posStand;

        // ripristino fisica
#if UNITY_600_OR_NEWER
        _rb.linearVelocity = Vector2.zero;
#else
        _rb.linearVelocity = Vector2.zero;
#endif
        _rb.angularVelocity = 0;
        _rb.bodyType = _origBodyType;
        _rb.gravityScale = keepZeroGravityAfter ? 0f : _origGravity;
        yield return new WaitForFixedUpdate();

        // riattiva animazioni
        Toggle(optionalAnimsToEnable, true);
        Toggle(optionalAnimsToDisable, true);

        // quarantena input
        if (inputDeadAfter > 0f) yield return new WaitForSeconds(inputDeadAfter);

        // hard stop + controlli on
        var motor = movementMotorToDisable as MovementMotor2D;
        if (motor) motor.HardStop();
        Toggle(inputSourcesToDisable, true);
        if (movementMotorToDisable) movementMotorToDisable.enabled = true;

        // risali duck
        if (duckMusic && AudioManagerMusic.I != null)
            AudioManagerMusic.I.SetUserVolume(1f, duckOutTime);

        _hasPlayedOnce = true;

        // stoppa eventuali one-shot SFX ancora in coda (passi, fruscii, ecc.)
        if (stopAllSfxOnEnd)
            AudioManagerSFX.I?.StopAll();

        // forza la lens alla size base per evitare conflitti collo zoom ancora in corsa
        if (useCameraZoom && cineCam) SetLensOrtho(cineCam, _baseOrtho);

        // micro shake “si rimette in piedi”
        if (shakeOnStand && cineCam)
            yield return StartCoroutine(CMShakeRoutine(shakeAmplitude, shakeDuration, shakeCycles));

        // chiudi
        ControlLock.Pop("PlayerWakeUp2D");
        IsRunning = false;
        enabled = false;
    }

    // --------- ZOOM CON CINEMACHINE 3 ---------
    IEnumerator CMZoomRoutine(float from, float to, float dur, bool presetAtStart = false, bool keepPpcDisabled = false)
    {
        if (!cineCam) yield break;

        bool reEnablePpc = false;
        if (disablePixelPerfectDuringZoom && _ppc && _ppc.enabled)
        {
            _ppc.enabled = false;
            reEnablePpc = true;
        }

        if (presetAtStart)
            SetLensOrtho(cineCam, from);

        if (dur <= 0f)
        {
            SetLensOrtho(cineCam, to);
        }
        else
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, dur);
                float v = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                SetLensOrtho(cineCam, v);
                yield return null;
            }
            SetLensOrtho(cineCam, to);
        }

        if (reEnablePpc) _ppc.enabled = true; // la riaccendiamo a fine zoom
    }

    IEnumerator CMShakeRoutine(float amplitude, float duration, int cycles)
    {
        if (!cineCam || duration <= 0f || amplitude <= 0f || cycles <= 0) yield break;

        bool reEnablePpc = false;
        if (disablePixelPerfectDuringZoom && _ppc && _ppc.enabled)
        {
            _ppc.enabled = false;
            reEnablePpc = true;
        }

        float t = 0f;
        float omega = Mathf.PI * 2f * cycles / Mathf.Max(0.0001f, duration);
        float baseSize = GetLensOrtho(cineCam);

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float env = (shakeEnvelope != null) ? Mathf.Clamp01(shakeEnvelope.Evaluate(k)) : (1f - k);
            float offset = Mathf.Sin(t * omega) * (amplitude * baseSize) * env;
            SetLensOrtho(cineCam, baseSize + offset);
            yield return null;
        }

        SetLensOrtho(cineCam, baseSize);
        if (reEnablePpc) _ppc.enabled = true;
    }

    // Helpers per Lens (struct!)
    static float GetLensOrtho(CinemachineCamera cam)
    {
        var lens = cam.Lens;
        return lens.OrthographicSize;
    }
    static void SetLensOrtho(CinemachineCamera cam, float value)
    {
        var lens = cam.Lens;
        lens.OrthographicSize = Mathf.Max(0.001f, value);
        cam.Lens = lens; // 🔑 riassegna lo struct alla v-cam
    }

    void Toggle(Behaviour[] list, bool on)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
            if (list[i]) list[i].enabled = on;
    }

#if UNITY_EDITOR
    [ContextMenu("Apply Overshoot Preset")]
    void ApplyOvershootPreset()
    {
        ease = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2.2f),
            new Keyframe(0.80f, 1.05f, 0f, 0f),
            new Keyframe(1f, 1f, -2.2f, 0f)
        );
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
