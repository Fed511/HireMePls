using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovementMotor2D : MonoBehaviour
{
    public float moveSpeed = 6f;
    [Range(0f, 1f)] public float acceleration = 0.2f; // 0 = istantaneo, 1 = molto morbido
    public bool faceMovement = false; // ruota lo sprite verso il movimento

    private Rigidbody2D _rb;
    private IInputSource2D _input;
    private Vector2 _desired;   // input desiderato
    private Vector2 _current;   // velocit� �target� smussata

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _input = GetComponent<IInputSource2D>(); // deve stare sullo stesso GO
        if (_input == null) Debug.LogWarning($"{name}: manca una IInputSource2D (es. PlayerAxisInput o AISeekInput).");
    }

    void Update()
    {
        if (_input != null) _desired = Vector2.ClampMagnitude(_input.GetMove(), 1f);
    }

    void FixedUpdate()
    {
        // smoothing semplice verso la direzione desiderata
        _current = Vector2.Lerp(_current, _desired * moveSpeed, 1f - Mathf.Pow(1f - acceleration, 60f * Time.fixedDeltaTime));
        _rb.linearVelocity = _current;

        if (faceMovement && _rb.linearVelocity.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;
            _rb.MoveRotation(angle);
        }
    }
}
