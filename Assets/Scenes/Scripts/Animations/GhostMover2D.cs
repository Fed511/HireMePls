using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class GhostMover2D : MonoBehaviour
{
    [Header("Moto sinusoidale")]
    public float speed = 2f;
    public float amplitude = 0.25f;
    public float frequency = 2.5f;

    [Header("Lifetime & Fade")]
    public float minAlive = 0.35f;      // non uccidere prima di X s
    public float lifetime = 6f;         // kill anche se non è uscito
    public float fadeDuration = 0.6f;   // fade-out in secondi
    public bool useFadeOut = true;


    // bounds (settati dallo spawner)
    float _killLeftX = float.NegativeInfinity;
    float _killMargin = 0.5f;

    // stato
    SpriteRenderer _sr;
    float _baseY, _phase, _t;
    bool _killing;
    Coroutine _fadeCo;

    public Action<GhostMover2D> onDespawn;

    void Awake() { _sr = GetComponent<SpriteRenderer>(); }

    /// Reinit per nuovo spawn
    public void Reinitialize(Vector3 startPos, float spd, float amp, float freq,
                             float killLeftX, float killMargin,
                             float life, float startAlpha = 0.9f, bool faceLeft = true)
    {
        transform.position = startPos;
        speed = spd; amplitude = amp; frequency = freq;
        _killLeftX = killLeftX; _killMargin = Mathf.Max(0f, killMargin);
        lifetime = Mathf.Max(0.1f, life);

        _t = 0f; _killing = false;
        if (_fadeCo != null) { StopCoroutine(_fadeCo); _fadeCo = null; }
        _phase = UnityEngine.Random.value * Mathf.PI * 2f;
        _baseY = startPos.y;

        if (_sr)
        {
            var c = _sr.color; c.a = Mathf.Clamp01(startAlpha); _sr.color = c;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _t += dt;

        // movimento
        transform.position += Vector3.left * (speed * dt);

        // onda
        _phase += frequency * dt;
        var p = transform.position;
        p.y = _baseY + Mathf.Sin(_phase) * amplitude;
        transform.position = p;

        if (_killing) return; // durante fade non controllare altro

        // kill per lifetime (dopo minAlive)
        if (_t >= Mathf.Max(minAlive, lifetime))
        {
            BeginKill();
            return;
        }

        // kill per uscita a sinistra (dopo minAlive)
        if (_t >= minAlive && transform.position.x < _killLeftX - _killMargin)
        {
            BeginKill();
        }
    }

    void BeginKill()
    {
        if (_killing) return;
        _killing = true;

        if (useFadeOut && _sr && fadeDuration > 0f)
        {
            _fadeCo = StartCoroutine(FadeAndDespawn());
        }
        else
        {
            onDespawn?.Invoke(this);
        }
    }

    IEnumerator FadeAndDespawn()
    {
        float t = 0f;
        var c0 = _sr.color;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            var c = c0; c.a = Mathf.Lerp(c0.a, 0f, t);
            _sr.color = c;
            yield return null;
        }
        onDespawn?.Invoke(this);
    }
}
