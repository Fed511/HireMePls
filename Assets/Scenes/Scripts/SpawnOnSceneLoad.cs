using UnityEngine;

public class SpawnOnSceneLoad : MonoBehaviour
{
    [Tooltip("Se vuoto userà il primo SpawnPoint trovato.")]
    public string fallbackSpawnId = "Default";

    [Tooltip("Se vuoto cerca automaticamente un oggetto col Tag 'Player'.")]
    public Transform player;

    [Header("Reset di sicurezza")]
    public bool clearControlLocks = true;   // sblocca eventuali lock rimasti (se non parte la cutscene)
    public bool forceTimeScaleOne = true;   // rimette il gioco a scorrere

    [Header("Fisica Player")]
    public bool forceDynamicBody = true;    // rimette il body in Dynamic
    public float gravityAfter = 0f;         // per top-down: 0

    [Header("Motor/Input")]
    public bool enableMotorAndInputAtStart = true; // abilita solo se non parte la cutscene

    void Start()
    {
        if (forceTimeScaleOne) Time.timeScale = 1f;

        // trova player se non assegnato
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }
        if (!player) { PlayerPersistence.NextSpawnId = null; return; }

        // sceglie ID
        string id = string.IsNullOrEmpty(PlayerPersistence.NextSpawnId)
            ? fallbackSpawnId
            : PlayerPersistence.NextSpawnId;

        // trova spawnpoint
        var points = GameObject.FindObjectsByType<SpawnPoint2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        SpawnPoint2D best = null;
        foreach (var p in points) { if (p && p.id == id) { best = p; break; } }
        if (!best && points.Length > 0) best = points[0];

        // posiziona player
        if (best) player.position = best.transform.position;

        // prepara fisica
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb)
        {
            if (forceDynamicBody) rb.bodyType = RigidbodyType2D.Dynamic;
#if UNITY_600_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.linearVelocity = Vector2.zero;
#endif
            rb.angularVelocity = 0f;
            rb.gravityScale = gravityAfter;
        }

        // determina se la cutscene partirà
        bool wakeWillRun = false;
        var wake = Object.FindFirstObjectByType<PlayerWakeUp2D>(FindObjectsInactive.Exclude);
        if (wake && wake.enabled && wake.runOnStart && (!wake.onlyFirstTime || !PlayerWakeUp2D.HasPlayedOnce))
            wakeWillRun = true;

        // clear lock solo se NON parte la cutscene (altrimenti rompe l'animazione)
        if (clearControlLocks && !wakeWillRun)
            ControlLock.ClearAll();

        // motor & input solo se NON parte la cutscene
        if (enableMotorAndInputAtStart && !wakeWillRun)
        {
            var motor = player.GetComponent<MovementMotor2D>();
            if (motor)
            {
                motor.enabled = true;
                motor.HardStop(); // azzera smoothing/stati
            }

            // ri-attiva eventuali sorgenti input che implementano IInputSource2D
            var comps = player.GetComponents<MonoBehaviour>();
            for (int i = 0; i < comps.Length; i++)
            {
                var mb = comps[i];
                if (!mb) continue;
                if (mb is IInputSource2D)
                {
                    var beh = mb as Behaviour;
                    if (beh && !beh.enabled) beh.enabled = true;
                }
            }
        }

        // orientamento consigliato (flip X) dal punto di spawn
        if (best)
        {
            var visual = player.GetComponentInChildren<SpriteRenderer>();
            if (visual && Mathf.Abs(best.faceDir.x) > 0.01f)
                visual.flipX = best.faceDir.x < 0f;
        }

        // consuma l'ID (sempre)
        PlayerPersistence.NextSpawnId = null;
    }
}
