using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Image))]
public class ScreenFader : MonoBehaviour
{
    public float fadeInTime = 0.3f;
    public float fadeOutTime = 0.3f;
    Image _img; float _t; bool _fadingIn;

    void Awake()
    {
        _img = GetComponent<Image>();
        _img.raycastTarget = true;        // blocca click durante il fade
        var c = _img.color; c.a = 1f;     // parte nero pieno → fade-in
        _img.color = c;
        _fadingIn = true; _t = 0f;
    }

    void Update()
    {
        if (_fadingIn)
        {
            _t += Time.unscaledDeltaTime / Mathf.Max(0.001f, fadeInTime);
            SetAlpha(1f - Mathf.Clamp01(_t));
            if (_t >= 1f) { _img.raycastTarget = false; _fadingIn = false; }
        }
    }

    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    System.Collections.IEnumerator FadeOutAndLoad(string sceneName)
    {
        _img.raycastTarget = true;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.001f, fadeOutTime);
            SetAlpha(Mathf.Clamp01(t));
            yield return null;
        }
        SceneManager.LoadScene(sceneName);
    }

    void SetAlpha(float a)
    {
        var c = _img.color; c.a = a; _img.color = c;
    }
}
