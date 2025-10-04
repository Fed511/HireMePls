using UnityEngine;

/// Footstep a distanza percorsa (robusto con MovePosition).
/// Richiede: Rigidbody2D sullo stesso GO. Usa AudioManagerSFX.I?.Play("footstep").
[RequireComponent(typeof(Rigidbody2D))]
public class FootstepSFX2D : MonoBehaviour
{
    [Header("Distanza tra passi (unità mondo)")]
    public float stepDistance = 0.75f;

    [Header("Soglia movimento")]
    public float minSpeed = 0.15f; // u/s

    [Header("SFX")]
    public string footstepId = "footstep";
    public bool alternateLeftRight = false;

    Rigidbody2D _rb;
    Vector2 _lastPos;
    float _accumDist;
    bool _left;

    // debug temporaneo
    public bool debugLog = true;
    float _debugUntil;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _lastPos = _rb.position;
        _debugUntil = Time.time + 10f; // logga solo i primi 10s
    }

    void FixedUpdate()
    {
        // distanza percorsa dall'ultimo tick fisico
        Vector2 now = _rb.position;
        float d = Vector2.Distance(now, _lastPos);
        float computedSpeed = d / Time.fixedDeltaTime;   // <-- velocità reale (u/s)
        _lastPos = now;

        if (debugLog && Time.time < _debugUntil)
            Debug.Log($"[Footstep] d={d:F3} speed={computedSpeed:F2} accum={_accumDist:F3}");

        // se troppo piano, scarica un po' l'accumulo per evitare "passi fantasma"
        if (computedSpeed < minSpeed)
        {
            _accumDist = Mathf.Max(0f, _accumDist - d * 0.5f);
            return;
        }

        _accumDist += d;
        if (_accumDist >= stepDistance)
        {
            _accumDist = 0f;
            PlayStep();
        }
    }

    void PlayStep()
    {
        string id = footstepId;
        if (alternateLeftRight)
        {
            string sided = id + (_left ? "_L" : "_R");
            if (!TryPlay(sided)) TryPlay(id);
            _left = !_left;
        }
        else
        {
            TryPlay(id);
        }
    }

    bool TryPlay(string id)
    {
        var mgr = AudioManagerSFX.I;
        if (mgr == null || string.IsNullOrEmpty(id))
        {
            if (debugLog && Time.time < _debugUntil)
                Debug.LogWarning("[Footstep] AudioManagerSFX.I nullo o id vuoto.");
            return false;
        }
        mgr.Play(id);
        if (debugLog && Time.time < _debugUntil)
            Debug.Log($"[Footstep] PLAY '{id}'");
        return true;
    }
}
