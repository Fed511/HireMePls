using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovementMotor2D : MonoBehaviour
{
    public float moveSpeed = 6f;
    [Range(0f, 1f)] public float acceleration = 0.2f; // 0 = scatto, 1 = molto morbido
    public bool faceMovement = false;
    public Transform visual; // opzionale: child da ruotare

    Rigidbody2D _rb;
    IInputSource2D _input;
    Vector2 _desired;   // direzione input normalizzata
    Vector2 _current;   // velocità attuale (u/s)

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _input = GetComponent<IInputSource2D>();
        if (_input == null)
            Debug.LogWarning($"{name}: manca una IInputSource2D (es. PlayerAxisInput o AISeekInput).");

        // impostazioni fisica raccomandate per un top-down 2D
        _rb.gravityScale = 0f;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        // NON freezare la rotazione se vuoi ruotare il body; se ruoti solo il visual, puoi freezare Z
    }
    public void HardStop()
    {
#if UNITY_600_OR_NEWER
    _rb.linearVelocity = Vector2.zero;
#else
        _rb.linearVelocity = Vector2.zero;
#endif
        _rb.angularVelocity = 0f;

        // azzera lo stato interno del motor
        _current = Vector2.zero;
        _desired = Vector2.zero;
    }

    void Update()
    {
        if (_input != null)
            _desired = Vector2.ClampMagnitude(_input.GetMove(), 1f);
    }

    void FixedUpdate()
    {
        // smoothing esponenziale verso la velocità target
        float k = 1f - Mathf.Pow(1f - acceleration, 60f * Time.fixedDeltaTime);
        Vector2 targetVel = _desired * moveSpeed;
        _current = Vector2.Lerp(_current, targetVel, k);

        // movimento fisico (rispetta i colliders): evita pass-through
        Vector2 nextPos = _rb.position + _current * Time.fixedDeltaTime;
        _rb.MovePosition(nextPos);

        // orientamento (solo grafica se possibile)
        if (faceMovement)
        {
            Vector2 v = _current;
            if (v.sqrMagnitude > 0.0001f)
            {
                float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                if (visual != null) visual.rotation = Quaternion.Euler(0, 0, ang);
                else _rb.MoveRotation(ang); // usa il body solo se non hai un visual separato
            }
        }
    }
}
