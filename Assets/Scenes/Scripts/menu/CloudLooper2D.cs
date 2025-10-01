using UnityEngine;

/// Muove la nuvola da sinistra→destra (o viceversa) e la riposiziona
/// dall'altro lato quando esce dallo schermo, con un leggero bobbing.
[RequireComponent(typeof(SpriteRenderer))]
public class CloudLooper2D : MonoBehaviour
{
    public float speed = 1f;              // unità/s (usa negativo per dx→sx)
    public float margin = 0.5f;           // margine extra fuori dallo schermo
    [Header("Bobbing (facoltativo)")]
    public float bobAmplitude = 0.05f;    // oscillazione verticale
    public float bobFrequency = 0.6f;

    [Header("Randomize ad ogni wrap")]
    public Vector2 yRange = new Vector2(-1f, 1f); // dove può comparire in Y
    public Vector2 speedRange = new Vector2(0.6f, 1.6f);
    public Vector2 scaleRange = new Vector2(0.9f, 1.3f);

    float _leftX, _rightX, _heightHalf;
    float _baseY, _phase;
    SpriteRenderer _sr;

    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        // Calcola i limiti in world space dalla camera principale
        var cam = Camera.main;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        _leftX = cam.transform.position.x - halfW - margin;
        _rightX = cam.transform.position.x + halfW + margin;

        // metà larghezza sprite (per spawn/margini più precisi)
        _heightHalf = _sr.bounds.extents.y;

        // setup bobbing
        _baseY = transform.position.y;
        _phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        // movimento orizzontale
        transform.position += Vector3.right * (speed * Time.deltaTime);

        // bobbing
        _phase += bobFrequency * Time.deltaTime;
        var p = transform.position;
        p.y = _baseY + Mathf.Sin(_phase) * bobAmplitude;
        transform.position = p;

        // wrapping: se esce a dx, rientra a sx (o viceversa)
        if (speed > 0f && transform.position.x - _sr.bounds.extents.x > _rightX)
            WrapToLeft();
        else if (speed < 0f && transform.position.x + _sr.bounds.extents.x < _leftX)
            WrapToRight();
    }

    void WrapToLeft()
    {
        // reset X a sinistra, random Y/scala/speed
        var pos = transform.position;
        pos.x = _leftX - _sr.bounds.extents.x;
        pos.y = Random.Range(yRange.x, yRange.y);
        transform.position = pos;

        RandomizeLook();
    }

    void WrapToRight()
    {
        var pos = transform.position;
        pos.x = _rightX + _sr.bounds.extents.x;
        pos.y = Random.Range(yRange.x, yRange.y);
        transform.position = pos;

        RandomizeLook();
    }

    void RandomizeLook()
    {
        // random scale uniforme (parallax fake)
        float s = Random.Range(scaleRange.x, scaleRange.y);
        transform.localScale = new Vector3(s * Mathf.Sign(transform.localScale.x), s, 1f);

        // random speed mantenendo la direzione
        float dir = Mathf.Sign(speed);
        speed = dir * Random.Range(speedRange.x, speedRange.y);

        // aggiorna base del bobbing
        _baseY = transform.position.y;
        _phase = Random.value * Mathf.PI * 2f;
    }
}
