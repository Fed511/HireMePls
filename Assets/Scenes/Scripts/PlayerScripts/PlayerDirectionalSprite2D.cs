using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerDirectionalSprite2D : MonoBehaviour
{
    [Header("Target grafico")]
    public SpriteRenderer target; // se vuoto usa quello sullo stesso GO

    [Header("Sprite")]
    public Sprite frontSprite;    // fronte (default)
    public Sprite backSprite;     // schiena (quando vai in su)

    [Header("Logica direzione")]
    [Tooltip("Sopra questa soglia Y consideriamo 'stai andando in su'.")]
    public float upThreshold = 0.35f;
    [Tooltip("Isteresi per evitare flicker quando scendi dalla soglia.")]
    public float hysteresis = 0.12f;

    [Tooltip("Se la velocità (o input) è sotto questa soglia, sei idle e NON cambiamo facing.")]
    public float idleThreshold = 0.08f;

    [Header("Flip orizzontale (opzionale)")]
    public bool flipByMoveX = true;
    public float sideThreshold = 0.1f;

    Rigidbody2D _rb;
    IInputSource2D _input;
    bool _showingBack; // stato persistente anche da fermi

    void Awake()
    {
        if (!target) target = GetComponent<SpriteRenderer>();
        _rb = GetComponentInParent<Rigidbody2D>() ?? GetComponent<Rigidbody2D>();
        _input = GetComponentInParent<IInputSource2D>() ?? GetComponent<IInputSource2D>();

        if (frontSprite && target) target.sprite = frontSprite;
    }

    void Update()
    {
        if (!target) return;
        if (ControlLock.IsLocked) return;

        Vector2 move = GetMoveVector(out float speed);
        float vy = move.y;
        float vx = move.x;

        // ——— blocco di decisione: NON cambiare stato se sei idle ———
        if (speed >= idleThreshold)
        {
            if (_showingBack)
            {
                // per uscire dalla schiena scendi sotto (soglia - isteresi)
                if (vy < upThreshold - hysteresis) _showingBack = false;
            }
            else
            {
                // per entrare in schiena supera la soglia
                if (vy > upThreshold) _showingBack = true;
            }
        }
        // se sei sotto idleThreshold: non tocchiamo _showingBack → resta com’era

        // applica sprite in base allo stato persistente
        var desired = _showingBack ? backSprite : frontSprite;
        if (desired && target.sprite != desired)
            target.sprite = desired;

        // flip orizzontale solo con movimento X sufficiente
        if (flipByMoveX && Mathf.Abs(vx) > sideThreshold)
            target.flipX = vx < 0f;
    }

    Vector2 GetMoveVector(out float speed)
    {
        // priorità alla velocità fisica
        if (_rb != null)
        {
#if UNITY_600_OR_NEWER
            var v = _rb.linearVelocity;
#else
            var v = _rb.linearVelocity;
#endif
            speed = v.magnitude;
            if (speed > 0.000001f) return v.normalized;
        }

        // fallback all'input
        if (_input != null)
        {
            var m = _input.GetMove();
            speed = m.magnitude;
            if (speed > 0.000001f) return m.normalized;
        }

        speed = 0f;
        return Vector2.zero;
    }
}
