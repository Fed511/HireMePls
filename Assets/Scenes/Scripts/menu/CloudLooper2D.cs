using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CloudLooperSimple : MonoBehaviour
{
    [Header("Movimento uniforme")]
    public float speed = 1f;             // +dx
    public float margin = 0.5f;          // quanto fuori schermo wrappare
    public float startOffsetUnits = 0f;  // offset fisso per questa nuvola (0, 2, 4 ...)

    [Header("Bobbing (0 = disattivato)")]
    public float bobAmplitude = 0f;      // es. 0.02 per un filo di vita
    public float bobFrequency = 0.6f;

    Camera _cam;
    SpriteRenderer _sr;
    float _leftX, _rightX;
    float _baseY;
    float _phase;

    void Awake()
    {
        _cam = Camera.main;
        _sr = GetComponent<SpriteRenderer>();
        _baseY = transform.position.y;
        _phase = Random.value * Mathf.PI * 2f;

        RecalcBounds();
        // Applica subito l'offset iniziale così le tre nuvole partono distanziate
        transform.position = new Vector3(
            transform.position.x - startOffsetUnits,
            transform.position.y,
            transform.position.z
        );
    }

    void Update()
    {
        if (_cam == null) return;

        RecalcBounds();

        // Movimento orizzontale
        transform.position += Vector3.right * (speed * Time.deltaTime);

        // Bobbing opzionale identico per tutte (se bobAmplitude=0, non fa nulla)
        if (bobAmplitude > 0f)
        {
            _phase += bobFrequency * Time.deltaTime;
            var p = transform.position;
            p.y = _baseY + Mathf.Sin(_phase) * bobAmplitude;
            transform.position = p;
        }

        // Wrap: appena esce a destra, rientra da sinistra mantenendo L'OFFSET
        float halfW = _sr.bounds.extents.x;
        if (transform.position.x - halfW > _rightX)
        {
            var pos = transform.position;
            pos.x = _leftX - halfW - startOffsetUnits; // ← mantiene distanza fissa
            transform.position = pos;
        }
    }

    void RecalcBounds()
    {
        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;
        _leftX = _cam.transform.position.x - halfW - margin;
        _rightX = _cam.transform.position.x + halfW + margin;
    }
}
