using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class MenuButton2D : MonoBehaviour
{
    public enum ButtonType { NewGame, LoadGame, Quit }

    [Header("Setup")]
    public ButtonType type = ButtonType.NewGame;
    [Tooltip("Scena di gioco (in Build Settings).")]
    public string nextSceneName = "FirstScene"; // usato per NewGame
    [Tooltip("SpawnPoint2D id nella scena di gioco.")]
    public string firstSpawnId = "Default";
    [Tooltip("Ordine nella navigazione (0=primo). Tieni valori consecutivi.")]
    public int navOrder = 0;

    [Header("Transizione (solo NewGame)")]
    public float fadeOut = 0.5f;
    public float fadeIn = 0.5f;
    public bool stopAllSfxOnStart = true;
    public bool stopMenuMusic = true;
    public float musicFadeOut = 0.35f;

    [Header("Feedback")]
    public Color baseColor = Color.white;
    public Color hoverColor = Color.white;
    public float hoverScale = 1.05f;
    public float tweenSpeed = 12f;

    [Header("Sicurezza")]
    public float clickCooldown = 0.25f;

    // --- shared state tra tutti i bottoni ---
    static readonly List<MenuButton2D> s_buttons = new List<MenuButton2D>();
    static int s_selectedIndex = 0;
    static MenuButton2D s_driver; // istanza che legge input da tastiera

    // --- istanza ---
    SpriteRenderer _sr;
    Vector3 _baseScale;
    bool _hover;
    float _lastClick;

    // ===== Lifecycle =====
    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale;

        var col = GetComponent<Collider2D>();
        col.isTrigger = false;

        _sr.color = baseColor;
    }

    void OnEnable()
    {
        if (!s_buttons.Contains(this))
        {
            s_buttons.Add(this);
            s_buttons.Sort((a, b) => a.navOrder.CompareTo(b.navOrder));
        }
        if (s_driver == null) s_driver = this;
        s_selectedIndex = Mathf.Clamp(s_selectedIndex, 0, s_buttons.Count - 1);
        RefreshVisuals();
    }

    void OnDisable()
    {
        int idx = s_buttons.IndexOf(this);
        if (idx >= 0) s_buttons.RemoveAt(idx);
        if (s_buttons.Count == 0) { s_driver = null; s_selectedIndex = 0; }
        else if (s_driver == this) s_driver = s_buttons[0];
    }

    void Update()
    {
        bool focused = (this == GetSelected()) || _hover;
        var targetColor = focused ? hoverColor : baseColor;
        var targetScale = focused ? _baseScale * hoverScale : _baseScale;

        _sr.color = Color.Lerp(_sr.color, targetColor, Time.unscaledDeltaTime * tweenSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * tweenSpeed);

        if (s_driver == this)
        {
            if (PressedUp()) MoveSelection(-1);
            if (PressedDown()) MoveSelection(+1);

            if (PressedSubmit())
                GetSelected()?.InvokeClick();
        }
    }

    // --- Input helpers ---
    bool PressedUp() => Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
    bool PressedDown() => Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
    bool PressedSubmit() => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);

    // ===== Mouse events =====
    void OnMouseEnter()
    {
        _hover = true;
        AudioManagerSFX.I?.Play("ui_hover");
        int idx = s_buttons.IndexOf(this);
        if (idx >= 0) { s_selectedIndex = idx; RefreshVisuals(); }
    }

    void OnMouseExit() { _hover = false; }

    void OnMouseUpAsButton() { InvokeClick(); }

    // ===== Actions =====
    void InvokeClick()
    {
        if (Time.unscaledTime - _lastClick < clickCooldown) return;
        _lastClick = Time.unscaledTime;

        switch (type)
        {
            case ButtonType.NewGame:
                if (string.IsNullOrEmpty(nextSceneName))
                {
                    Debug.LogWarning("nextSceneName non impostato su MenuButton2D.");
                    return;
                }
                AudioManagerSFX.I?.Play("ui_click");

                // imposta lo spawn per la prima scena
                PlayerPersistence.NextSpawnId = firstSpawnId;

                // audio menu
                if (stopAllSfxOnStart) AudioManagerSFX.I?.StopAll();
                if (stopMenuMusic) AudioManagerMusic.I?.Stop(musicFadeOut);

                // usa fader se presente (ATTENZIONE: è una coroutine!)
                var fader = ScreenFader.I ?? FindFirstObjectByType<ScreenFader>(FindObjectsInactive.Exclude);
                if (fader)
                {
                    StartCoroutine(fader.FadeToScene(nextSceneName, fadeOut, fadeIn));
                }
                else
                {
                    // fallback senza fader
                    StartCoroutine(LoadSceneNoFader(nextSceneName, fadeOut, fadeIn));
                }
                break;

            case ButtonType.LoadGame:
                AudioManagerSFX.I?.Play("ui_click");
                Debug.Log("LoadGame: da implementare.");
                break;

            case ButtonType.Quit:
#if UNITY_EDITOR
                AudioManagerSFX.I?.Play("ui_error");
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }

    IEnumerator LoadSceneNoFader(string sceneName, float outDur, float inDur)
    {
        if (outDur > 0f) yield return new WaitForSeconds(outDur);
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!op.isDone) yield return null;
        if (inDur > 0f) yield return new WaitForSeconds(inDur);
    }

    // ===== Selection logic =====
    static MenuButton2D GetSelected()
    {
        if (s_buttons.Count == 0) return null;
        s_selectedIndex = Mathf.Clamp(s_selectedIndex, 0, s_buttons.Count - 1);
        return s_buttons[s_selectedIndex];
    }

    static void MoveSelection(int dir)
    {
        if (s_buttons.Count == 0) return;
        s_selectedIndex = (s_selectedIndex + dir + s_buttons.Count) % s_buttons.Count;
        AudioManagerSFX.I?.Play("ui_hover");
        foreach (var b in s_buttons) b.RefreshVisuals();
    }

    void RefreshVisuals()
    {
        bool focused = (this == GetSelected()) || _hover;
        _sr.color = focused ? hoverColor : baseColor;
        transform.localScale = focused ? _baseScale * hoverScale : _baseScale;
    }
}
