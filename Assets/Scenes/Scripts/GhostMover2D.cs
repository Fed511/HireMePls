using UnityEngine;
using System;

[RequireComponent(typeof(SpriteRenderer))]
public class GhostMover2D : MonoBehaviour
{
    [Header("Moto sinusoidale")]
    public float speed = 2f;         // unità/s a sx
    public float amplitude = 0.25f;  // altezza dell’onda
    public float frequency = 2.5f;   // Hz

    [Header("Fade")]
    public bool fadeOutOnKill = true;
    public float fadeSpeed = 2f;     // unità di alpha/s

    [Header("Aspetto")]
    public bool faceLeft = true;     // flippa a sinistra se true

    // bounds per kill a coordinate (settati dallo spawner)
    float _killLeftX = float.NegativeInfinity;
    float _killMargin = 0.5f;

    // lifetime (settato dallo spawner)
    float _lifetime = 6f;
    float _t;

    // stato interno
    SpriteRenderer _sr;
    float _baseY, _phase;
    bool _killing;

    // callback verso lo spawner
    public Action<GhostMover2D> onDespawn;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    /// Reinizializza per un nuovo spawn (chiamata dallo spawner/pool)
    public void Reinitialize(Vector3 startPos, float speed, float amp, float freq,
                             float killLeftX, float killMargin,
                             float lifetime, float startAlpha = 0.9f, bool faceLeft = true)
    {
        transform.position = startPos;
        this.speed = speed;
        amplitude = amp;
        frequency = freq;
        _killLeftX = killLeftX;
        _killMargin = Mathf.Max(0f, killMargin);
        _lifetime = Mathf.Max(0.1f, lifetime);

        _t = 0f;
        _killing = false;
        _phase = UnityEngine.Random.value * Mathf.PI * 2f;
        _baseY = startPos.y;
        this.faceLeft = faceLeft;

        if (_sr != null)
        {
            var c = _sr.color;
            c.a = Mathf.Clamp01(startAlpha);
            _sr.color = c;
            _sr.flipX = this.faceLeft; // guardare a sx
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _t += dt;

        // movimento a sinistra
        transform.position += Vector3.left * (speed * dt);

        // bobbing
        _phase += frequency * dt;
        var p = transform.position;
        p.y = _baseY + Mathf.Sin(_phase) * amplitude;
        transform.position = p;

        // lifetime
        if (!_killing && _t >= _lifetime)
            BeginKill();

        // kill a coordinate
        if (!_killing && transform.position.x < _killLeftX - _killMargin)
            BeginKill();

        // fade & despawn
        if (_killing)
        {
            if (!fadeOutOnKill) { Despawn(); return; }

            if (_sr)
            {
                var c = _sr.color;
                c.a = Mathf.MoveTowards(c.a, 0f, fadeSpeed * dt);
                _sr.color = c;
                if (c.a <= 0.001f) Despawn();
            }
            else Despawn();
        }
    }

    void BeginKill()
    {
        if (_killing) return;
        _killing = true;
    }

    void Despawn()
    {
        onDespawn?.Invoke(this); // lo spawner ci rimette nel pool
    }
}
