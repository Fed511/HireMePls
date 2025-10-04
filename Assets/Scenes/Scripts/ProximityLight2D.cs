using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
[RequireComponent(typeof(Collider2D))]
public class ProximityLight2D : MonoBehaviour
{
    [Header("Trigger")]
    public string playerTag = "Player";
    public LayerMask triggerLayers = ~0; // opzionale

    [Header("Luce")]
    public float onIntensity = 1.2f;
    public float offIntensity = 0f;
    public Color onColor = new Color(1f, 0.92f, 0.75f);
    public Color offColor = new Color(0.9f, 0.85f, 0.7f, 0.6f);

    [Header("Fades")]
    public float fadeIn = 0.25f;
    public float fadeOut = 0.6f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Auto-Off")]
    public bool autoTurnOff = true;
    public float holdTime = 1.5f; // quanto resta accesa dopo l’uscita

    [Header("SFX (opz)")]
    public string sfxOn = "";   // es. "light_on"
    public string sfxOff = "";   // es. "light_off"

    Light2D _light;
    Collider2D _col;
    Coroutine _fadeCo;
    Coroutine _delayedOffCo;
    float _currentTarget;
    Color _currentColorTarget;
    float _lastEnterTime;

    void Awake()
    {
        _light = GetComponent<Light2D>();
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;

        _light.intensity = offIntensity;
        _light.color = offColor;
    }

    void OnDisable()
    {
        // Se il GO si disattiva (es. cambio scena), stoppa safely le coroutine locali
        if (_delayedOffCo != null) { StopCoroutine(_delayedOffCo); _delayedOffCo = null; }
        if (_fadeCo != null) { StopCoroutine(_fadeCo); _fadeCo = null; }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        _lastEnterTime = Time.time;

        // Se stavi aspettando lo spegnimento, annullalo
        if (_delayedOffCo != null) { StopCoroutine(_delayedOffCo); _delayedOffCo = null; }

        FadeTo(onIntensity, onColor, fadeIn);
        if (!string.IsNullOrEmpty(sfxOn)) AudioManagerSFX.I?.Play(sfxOn);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        _lastEnterTime = Time.time; // finché il player sta dentro, non spegnere
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (!autoTurnOff) return;

        // evita di lanciare coroutine se l'oggetto è inattivo o disabilitato
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;

        // una sola coroutine di delayed-off alla volta
        if (_delayedOffCo != null) StopCoroutine(_delayedOffCo);
        _delayedOffCo = StartCoroutine(DelayedOff());
    }

    IEnumerator DelayedOff()
    {
        float deadline = Time.time + holdTime;
        while (Time.time < deadline)
        {
            // se l'oggetto viene disattivato durante l'attesa, esci senza errori
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy) yield break;
            yield return null;
        }

        FadeTo(offIntensity, offColor, fadeOut);
        if (!string.IsNullOrEmpty(sfxOff)) AudioManagerSFX.I?.Play(sfxOff);
        _delayedOffCo = null;
    }

    void FadeTo(float targetIntensity, Color targetColor, float duration)
    {
        _currentTarget = targetIntensity;
        _currentColorTarget = targetColor;

        if (_fadeCo != null) { StopCoroutine(_fadeCo); _fadeCo = null; }

        // se disabilitato o fuori gerarchia, applica direttamente e basta
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy || duration <= 0f)
        {
            _light.intensity = _currentTarget;
            _light.color = _currentColorTarget;
            return;
        }

        _fadeCo = StartCoroutine(FadeRoutine(duration));
    }

    IEnumerator FadeRoutine(float dur)
    {
        float fromI = _light.intensity;
        Color fromC = _light.color;

        float t = 0f;
        while (t < 1f)
        {
            // se nel frattempo il GO viene disattivato → uscita silenziosa
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy) yield break;

            t += Time.deltaTime / Mathf.Max(0.0001f, dur);
            float k = curve.Evaluate(Mathf.Clamp01(t));
            _light.intensity = Mathf.Lerp(fromI, _currentTarget, k);
            _light.color = Color.Lerp(fromC, _currentColorTarget, k);
            yield return null;
        }

        _light.intensity = _currentTarget;
        _light.color = _currentColorTarget;
        _fadeCo = null;
    }

    bool IsPlayer(Collider2D c)
    {
        if (((1 << c.gameObject.layer) & triggerLayers) == 0) return false;
        if (!string.IsNullOrEmpty(playerTag) && !c.CompareTag(playerTag)) return false;
        return true;
    }
}
