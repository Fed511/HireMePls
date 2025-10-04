using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(Image))]
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader I { get; private set; }

    [Header("Durate di default")]
    public float fadeInTime = 0.30f;
    public float fadeOutTime = 0.30f;

    [Header("Aspetto")]
    public Color overlayColor = Color.black;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Avvio")]
    [Tooltip("Se true: parte nero e fa automaticamente FadeIn all'avvio.")]
    public bool autoFadeInOnStart = true;

    Image _img;
    bool _busy;
    float _alpha;   // stato corrente (persistente tra scene)

    void Awake()
    {
        // Singleton persistente cross-scene
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        _img = GetComponent<Image>();
        _img.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 1f);
        _img.raycastTarget = true; // blocca input quando visibile

        // Assicura overlay sempre sopra tutto
        var canvas = GetComponentInParent<Canvas>();
        if (canvas) { canvas.sortingOrder = 9999; canvas.overrideSorting = true; }

        _alpha = 1f; // partiamo neri; se non vuoi, metti autoFadeInOnStart=false e chiama tu FadeIn
    }

    void Start()
    {
        if (autoFadeInOnStart) StartCoroutine(FadeIn());
        else SetAlpha(_alpha); // mantieni stato
    }

    // -------- API principali --------

    public IEnumerator FadeIn(float dur = -1f)
    {
        if (dur < 0f) dur = fadeInTime;
        yield return FadeTo(0f, dur);
        _img.raycastTarget = false; // libero i click
    }

    public IEnumerator FadeOut(float dur = -1f)
    {
        if (dur < 0f) dur = fadeOutTime;
        _img.raycastTarget = true;
        yield return FadeTo(1f, dur);
    }

    public void InstantHide() { SetAlpha(0f); _img.raycastTarget = false; }
    public void InstantShow() { SetAlpha(1f); _img.raycastTarget = true; }

    // Facile: fade out → load async → fade in
    public IEnumerator FadeToScene(string sceneName, float outDur = -1f, float inDur = -1f)
    {
        yield return FadeOut(outDur);
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!op.isDone) yield return null;
        yield return FadeIn(inDur);
    }

    // -------- Interni --------

    IEnumerator FadeTo(float target, float dur)
    {
        if (_busy) yield break;
        _busy = true;

        float start = _alpha;
        if (dur <= 0f) { SetAlpha(target); _busy = false; yield break; }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float k = ease.Evaluate(Mathf.Clamp01(t));
            SetAlpha(Mathf.Lerp(start, target, k));
            yield return null;
        }
        SetAlpha(target);
        _busy = false;
    }

    void SetAlpha(float a)
    {
        _alpha = Mathf.Clamp01(a);
        var c = overlayColor; c.a = _alpha;
        _img.color = c;
        _img.raycastTarget = _alpha > 0.001f;
    }
}
