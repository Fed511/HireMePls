using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class ScenePortal2D : MonoBehaviour
{
    [Header("Destinazione")]
    [Tooltip("Nome esatto della scena di arrivo (in Build Settings).")]
    public string toSceneName;
    [Tooltip("ID dello SpawnPoint2D nella scena di arrivo.")]
    public string toSpawnId = "Default";

    [Header("Attivazione")]
    [Tooltip("Entra appena il Player tocca il trigger.")]
    public bool autoEnterOnTouch = true;
    [Tooltip("Se attivo, richiede di premere un tasto mentre si è nel trigger.")]
    public bool requireButton = false;
    public KeyCode useKey = KeyCode.E;
    public string playerTag = "Player";

    [Header("Transizione")]
    public float fadeOut = 0.35f;
    public float fadeIn = 0.35f;
    public string sfxEnter = "portal"; // opzionale, lascia vuoto se non vuoi suono
    [Tooltip("Ignora nuovi ingressi per X secondi dopo l'uso.")]
    public float cooldown = 0.2f;

    bool _busy;
    float _lastUseTime;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // assicura che sia trigger in editor
        var c = GetComponent<Collider2D>();
        if (c && !c.isTrigger) c.isTrigger = true;
    }
#endif

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoEnterOnTouch) return;
        if (!CanUse(other)) return;
        StartCoroutine(DoTransit());
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!requireButton) return;
        if (!CanUse(other)) return;
        if (Input.GetKeyDown(useKey)) StartCoroutine(DoTransit());
    }

    bool CanUse(Collider2D other)
    {
        if (_busy) return false;
        if (Time.unscaledTime - _lastUseTime < cooldown) return false;
        if (!other || !other.CompareTag(playerTag)) return false;
        if (string.IsNullOrEmpty(toSceneName)) return false;
        return true;
    }

    IEnumerator DoTransit()
    {
        _busy = true;
        _lastUseTime = Time.unscaledTime;

        // Lock controlli globali (se usi il tuo ControlLock)
        ControlLock.Push("Portal2D");

        // SFX ingresso (one-shot)
        if (!string.IsNullOrEmpty(sfxEnter))
            AudioManagerSFX.I?.Play(sfxEnter);

        // Comunica dove spawnare nella scena di arrivo
        PlayerPersistence.NextSpawnId = toSpawnId;

        // Fade out (se c'è un fader persistente lo usa, altrimenti fallback a attesa)
        var fader = ScreenFader.I ?? FindFirstObjectByType<ScreenFader>(FindObjectsInactive.Exclude);
        if (fader) yield return fader.FadeOut(fadeOut);
        else if (fadeOut > 0f) yield return new WaitForSeconds(fadeOut);

        // Stoppa SFX residui (passi, ambience one-shot, ecc.)
        AudioManagerSFX.I?.StopAll();

        // Carica la scena in async
        var op = SceneManager.LoadSceneAsync(toSceneName, LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        // Fade in nella nuova scena (se disponibile)
        var fader2 = ScreenFader.I ?? FindFirstObjectByType<ScreenFader>(FindObjectsInactive.Exclude);
        if (fader2) yield return fader2.FadeIn(fadeIn);
        else if (fadeIn > 0f) yield return new WaitForSeconds(fadeIn);

        // Sblocca controlli
        ControlLock.Pop("Portal2D");
        _busy = false;
    }
}
