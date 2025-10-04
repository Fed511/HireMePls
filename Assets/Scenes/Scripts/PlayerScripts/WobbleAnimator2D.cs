using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class WobbleAnimator2D : MonoBehaviour
{
    [Header("Target grafico (child)")]
    public Transform target; // assegna Player/Visual
    public bool autoFindChildSprite = true;

    [Header("Trigger movimento")]
    public float moveThreshold = 0.05f;

    [Header("Oscillazione")]
    public float frequency = 6f;
    public float rotAmplitudeDeg = 6f;
    public float bobAmplitude = 0.03f;

    [Header("Squash & Stretch")]
    [Range(0f, 0.3f)] public float squashAmount = 0.08f;

    [Header("Aspetto")]
    public bool flipByMoveX = false;
    public float smoothReturn = 12f;

    Rigidbody2D _rb;
    Vector3 _baseScale;
    Vector3 _baseLocalPos;
    float _phase;
    SpriteRenderer _sr;

    // cache velocit� calcolata in FixedUpdate (compatibile con MovePosition)
    Vector2 _vel;       // u/s
    float _speed;       // magnitudine
    Vector2 _lastPos;   // posizione del rb nell'ultimo FixedUpdate

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (target == null && autoFindChildSprite)
        {
            _sr = GetComponentInChildren<SpriteRenderer>(true);
            if (_sr != null) target = _sr.transform;
        }
        if (target == null)
        {
            Debug.LogWarning($"{name}: WobbleAnimator2D senza target. Assegna il child grafico (es. Player/Visual).");
            enabled = false;
            return;
        }

        _sr = target.GetComponent<SpriteRenderer>();
        _baseScale = target.localScale;
        _baseLocalPos = target.localPosition;

        _lastPos = _rb.position;
    }

    void FixedUpdate()
    {
        // 1) prova a prendere la velocity del RB (Unity la aggiorna anche con MovePosition)
        _vel = _rb.linearVelocity;

        // 2) se per qualche motivo � (quasi) zero, calcolala manualmente
        if (_vel.sqrMagnitude < 0.000001f)
        {
            Vector2 now = _rb.position;
            _vel = (now - _lastPos) / Time.fixedDeltaTime;
            _lastPos = now;
        }

        _speed = _vel.magnitude;
    }

    void Update()
    {
        bool moving = _speed > moveThreshold;

        if (moving)
            _phase += frequency * Mathf.Clamp01(_speed) * Time.deltaTime;
        else
            _phase = Mathf.Lerp(_phase, 0f, Time.deltaTime * (smoothReturn * 0.5f));

        float vigor = Mathf.Clamp01(_speed);
        float s = Mathf.Sin(_phase);
        float sAbs = Mathf.Abs(s);

        float targetRot = s * rotAmplitudeDeg * vigor;
        float targetBob = (sAbs * 2f - 1f) * bobAmplitude * vigor;

        float squash = squashAmount * vigor * -s;
        Vector3 targetScale = new Vector3(
            _baseScale.x * (1f + -squash),
            _baseScale.y * (1f + squash),
            _baseScale.z
        );

        // Applica SOLO al child grafico
        target.localRotation = Quaternion.Lerp(
            target.localRotation,
            Quaternion.Euler(0f, 0f, targetRot),
            Time.deltaTime * (moving ? 20f : smoothReturn)
        );

        target.localPosition = Vector3.Lerp(
            target.localPosition,
            _baseLocalPos + new Vector3(0f, targetBob, 0f),
            Time.deltaTime * (moving ? 20f : smoothReturn)
        );

        target.localScale = Vector3.Lerp(
            target.localScale,
            targetScale,
            Time.deltaTime * (moving ? 14f : smoothReturn)
        );

        // Flip opzionale solo grafico
        if (flipByMoveX && _sr != null)
        {
            var vx = _vel.x;
            if (Mathf.Abs(vx) > 0.01f)
                _sr.flipX = vx < 0f;
        }
    }
}
