using System;
using UnityEngine;

/// Movimento "foglio che svolazza": drift verso l'alto, dondolio, rotazione, fade e auto-destroy.
public class PaperFloat2D : MonoBehaviour
{
    [Header("Drift")]
    public Vector2 upwardSpeedRange = new Vector2(0.7f, 1.4f); // u/s
    public Vector2 horizontalDriftRange = new Vector2(-0.35f, 0.35f);

    [Header("Wobble / Rotazione")]
    public float wobbleFrequency = 3.5f;  // Hz
    public float wobbleAmplitude = 0.05f; // unità locali su Y
    public Vector2 spinDegPerSecRange = new Vector2(-90f, 90f);

    [Header("Vita & Fade")]
    public Vector2 lifetimeRange = new Vector2(3.5f, 6.0f);
    public float fadeOutTime = 0.5f;

    [Header("Scala")]
    public Vector2 scaleRange = new Vector2(0.85f, 1.15f);
    public bool flipRandomX = true;

    [HideInInspector] public Action<GameObject> onFinished; // lo setta lo spawner

    // stato
    float _t, _life, _phase;
    float _up, _dx, _spin;
    Vector3 _basePos, _baseScale;
    SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
        if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        // se riattivato direttamente senza passare da Reinitialize
        if (_life <= 0f) Reinitialize();
    }

    public void Reinitialize()
    {
        _t = 0f;
        _life = UnityEngine.Random.Range(lifetimeRange.x, lifetimeRange.y);
        _phase = UnityEngine.Random.value * Mathf.PI * 2f;

        _up = UnityEngine.Random.Range(upwardSpeedRange.x, upwardSpeedRange.y);
        _dx = UnityEngine.Random.Range(horizontalDriftRange.x, horizontalDriftRange.y);
        _spin = UnityEngine.Random.Range(spinDegPerSecRange.x, spinDegPerSecRange.y);

        _basePos = transform.position;
        _baseScale = Vector3.one * UnityEngine.Random.Range(scaleRange.x, scaleRange.y);
        if (_sr) _sr.flipX = flipRandomX && UnityEngine.Random.value < 0.5f;

        transform.localScale = _baseScale;

        // alpha piena all'inizio
        if (_sr) { var c = _sr.color; c.a = 1f; _sr.color = c; }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _t += dt;
        _phase += wobbleFrequency * dt;

        // Movimento
        Vector3 p = _basePos;
        p.x += _dx * _t;
        p.y += _up * _t + Mathf.Sin(_phase) * wobbleAmplitude;
        transform.position = p;

        // Rotazione
        transform.Rotate(0f, 0f, _spin * dt, Space.Self);

        // Fade e fine
        float remain = _life - _t;
        if (remain <= fadeOutTime && _sr != null)
        {
            float a = Mathf.InverseLerp(0f, fadeOutTime, Mathf.Max(0f, remain));
            var c = _sr.color; c.a = a; _sr.color = c;
        }

        if (_t >= _life)
        {
            if (onFinished != null) onFinished(gameObject);
            else Destroy(gameObject);
        }
    }

    void OnBecameInvisible()
    {
        // sicurezza: se esce dallo schermo, termina prima
        if (gameObject.activeInHierarchy)
        {
            if (onFinished != null) onFinished(gameObject);
            else Destroy(gameObject);
        }
    }
}
