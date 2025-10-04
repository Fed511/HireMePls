using UnityEngine;

public class IdleBreathing2D : MonoBehaviour
{
    [Header("Frequenza/ampiezza del respiro")]
    public float idleFrequency = 1.2f;       // ~1�2 respiri/sec
    [Range(0f, 0.2f)] public float breathScale = 0.035f; // scala (�3.5% default)
    public float idleBobAmplitude = 0.01f;   // piccolo bobbing su Y

    [Header("Blending con movimento (opzionale)")]
    public Rigidbody2D speedSource;          // se nullo, lo cerca nel parent
    public float speedForFullSuppression = 6f; // a questa velocit� il respiro ~0
    public float smooth = 12f;               // ritorno morbido a neutro

    [Header("Cosa animare")]
    public bool affectScale = true;
    public bool affectBob = true;

    Vector3 _baseScale;
    Vector3 _baseLocalPos;
    float _phase;

    void Awake()
    {
        if (speedSource == null)
            speedSource = GetComponentInParent<Rigidbody2D>();

        _baseScale = transform.localScale;
        _baseLocalPos = transform.localPosition;

        // desincronizza leggermente istanze multiple
        _phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        // 1) quanto dobbiamo "vedere" il respiro
        float speed = (speedSource != null) ? speedSource.linearVelocity.magnitude : 0f;
        float suppress = Mathf.Clamp01(speed / Mathf.Max(0.001f, speedForFullSuppression));
        float breathBlend = 1f - suppress; // 1 = fermo, 0 = in corsa

        // 2) fase e seno del respiro
        _phase += idleFrequency * Time.deltaTime;
        float s = Mathf.Sin(_phase);

        // 3) target scale/pos
        Vector3 targetScale = _baseScale;
        if (affectScale)
        {
            // allunga Y e stringe X in controfase
            float x = _baseScale.x * (1f - breathScale * s * breathBlend);
            float y = _baseScale.y * (1f + breathScale * s * breathBlend);
            targetScale = new Vector3(x, y, _baseScale.z);
        }

        Vector3 targetPos = _baseLocalPos;
        if (affectBob)
        {
            targetPos += new Vector3(0f, s * idleBobAmplitude * breathBlend, 0f);
        }

        // 4) applica morbido
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * smooth);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * smooth);
    }
}
