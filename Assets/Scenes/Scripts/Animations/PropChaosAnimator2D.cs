using UnityEngine;

/// Movimento "pazzo" da cartoon: dondolio forte + rimbalzi casuali.
/// Mettilo sul root dell'oggetto; assegna come target il child grafico (Visual).
public class PropChaosAnimator2D : MonoBehaviour
{
    [Header("Target grafico (child)")]
    public Transform target;                 // es. Tree/Hinge o House/Visual
    public bool autoFindChildSprite = true;  // trova primo SpriteRenderer nei figli

    [Header("Dondolio continuo (sin)")]
    [Tooltip("Hz medi del dondolio base")]
    public float baseFrequency = 0.8f;
    [Tooltip("± gradi di rotazione Z")]
    public float rotAmplitudeDeg = 10f;
    [Tooltip("ampiezza bobbing verticale in unità locali")]
    public float bobAmplitude = 0.03f;
    [Range(0f, 0.35f)] public float squashAmount = 0.08f;

    [Header("Impulsi / Rimbalzi casuali (burst)")]
    [Tooltip("Quante raffiche al secondo in media")]
    public float burstRate = 0.6f;
    [Tooltip("Forza media del burst (scala tutto il pacchetto)")]
    public float burstStrength = 1.0f;
    [Tooltip("Smorzamento della molla (0=instabile, 1=stop immediato)")]
    [Range(0f, 1f)] public float springDamping = 0.25f;
    [Tooltip("Rigidezza della molla che riporta a 0")]
    public float springStiffness = 24f;

    [Header("Aspetto / Controllo")]
    public bool pivotAtBase = true;          // consigliato per alberi/pali
    public float smooth = 18f;               // lerp verso il target
    public bool desyncInstances = true;      // fasi diverse tra istanze
    public int seed = 0;                     // 0 = random, >0 = deterministico

    // cache
    SpriteRenderer _sr;
    Vector3 _basePos, _baseScale;
    float _phase;
    System.Random _rng;

    // stato molla (burst)
    float _rotKick;   // offset rotazione extra (deg)
    float _rotVel;    // vel della molla (deg/s)
    float _bobKick;   // offset bob extra (u)
    float _bobVel;    // vel bob (u/s)
    float _squashKick; // offset squash extra
    float _squashVel;

    // timer per bursts
    float _nextBurstT;

    void Awake()
    {
        if (target == null && autoFindChildSprite)
        {
            _sr = GetComponentInChildren<SpriteRenderer>(true);
            if (_sr) target = _sr.transform;
        }
        if (target == null) { enabled = false; Debug.LogWarning($"{name}: PropChaosAnimator2D senza target."); return; }
        if (_sr == null) _sr = target.GetComponent<SpriteRenderer>();

        _basePos = target.localPosition;
        _baseScale = target.localScale;

        int realSeed = seed != 0 ? seed : (desyncInstances ? (GetInstanceID() ^ System.Environment.TickCount) : 12345);
        _rng = new System.Random(realSeed);

        _phase = desyncInstances ? (float)_rng.NextDouble() * Mathf.PI * 2f : 0f;
        ScheduleNextBurst(Time.time);
    }

    void Update()
    {
        // 1) dondolio base (sin)
        _phase += baseFrequency * Time.deltaTime;
        float s = Mathf.Sin(_phase);
        float sAbs = Mathf.Abs(s);

        float rotBase = s * rotAmplitudeDeg;
        float bobBase = (sAbs * 2f - 1f) * bobAmplitude;  // ondina verticale
        float squashBase = -s * squashAmount;

        // 2) bursts casuali → integrazione molla smorzata
        UpdateSpring(ref _rotKick, ref _rotVel, springStiffness, springDamping);
        UpdateSpring(ref _bobKick, ref _bobVel, springStiffness, springDamping);
        UpdateSpring(ref _squashKick, ref _squashVel, springStiffness, springDamping);

        if (Time.time >= _nextBurstT)
        {
            DoBurst();                       // applica un colpo casuale
            ScheduleNextBurst(Time.time);    // programma il prossimo
        }

        // 3) target composito (base + kick)
        float rotZ = rotBase + _rotKick;
        float bobY = bobBase + _bobKick;
        float squ = squashBase + _squashKick;

        Vector3 posTarget = _basePos + new Vector3(0, bobY, 0);
        Quaternion rotTarget = Quaternion.Euler(0, 0, rotZ);
        Vector3 scaleTarget = new Vector3(
            _baseScale.x * (1f - squ),
            _baseScale.y * (1f + squ),
            _baseScale.z
        );

        // 4) micro-compensazione pivot alla base
        if (pivotAtBase && _sr != null)
        {
            float h = _sr.bounds.size.y * target.lossyScale.y;
            posTarget += new Vector3(0f, -Mathf.Abs(Mathf.Sin(rotZ * Mathf.Deg2Rad)) * h * 0.03f, 0f);
        }

        // 5) applica al SOLO child grafico
        target.localRotation = Quaternion.Lerp(target.localRotation, rotTarget, Time.deltaTime * smooth);
        target.localPosition = Vector3.Lerp(target.localPosition, posTarget, Time.deltaTime * smooth);
        target.localScale = Vector3.Lerp(target.localScale, scaleTarget, Time.deltaTime * smooth);
    }

    void UpdateSpring(ref float x, ref float v, float k, float d)
    {
        // x'' + 2ζω x' + ω^2 x = 0  (integrazione semi-implicita semplice)
        float dt = Time.deltaTime;
        // forza = -k*x - c*v
        float c = 2f * d * Mathf.Sqrt(k); // smorzamento critico scalabile
        float a = (-k * x - c * v);
        v += a * dt;
        x += v * dt;
    }

    void DoBurst()
    {
        // direzioni random e ampiezze coerenti
        float r1 = (float)_rng.NextDouble() * 2f - 1f; // -1..1
        float r2 = (float)_rng.NextDouble() * 2f - 1f;
        float r3 = (float)_rng.NextDouble() * 2f - 1f;

        // scalatura in funzione dell'ampiezza base (così "si somma" bene)
        _rotVel += r1 * rotAmplitudeDeg * 8f * burstStrength;     // colpo su rotazione
        _bobVel += r2 * bobAmplitude * 40f * burstStrength;   // colpo su bob
        _squashVel += r3 * squashAmount * 20f * burstStrength;   // colpo su squash
    }

    void ScheduleNextBurst(float now)
    {
        if (burstRate <= 0f) { _nextBurstT = float.PositiveInfinity; return; }
        // tempo attesa ~ esponenziale per jitter naturale
        float mean = 1f / burstRate;
        float u = Mathf.Clamp01((float)_rng.NextDouble());
        float wait = -Mathf.Log(1f - u) * mean; // distribuzione esponenziale
        _nextBurstT = now + wait;
    }
}
