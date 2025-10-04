using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-250)]
public class PlayerPersistence : MonoBehaviour
{
    public static PlayerPersistence I { get; private set; }

    // quale spawn usare nella prossima scena (es. impostato da un portale)
    public static string NextSpawnId = "Default";

    Rigidbody2D _rb;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        _rb = GetComponent<Rigidbody2D>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (I == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        // trova lo spawn point con l'ID richiesto
        var spawns = GameObject.FindObjectsOfType<SpawnPoint2D>(true);
        SpawnPoint2D target = null;
        foreach (var sp in spawns)
            if (sp.id == NextSpawnId) { target = sp; break; }
        if (target == null && spawns.Length > 0) target = spawns[0]; // fallback

        if (target)
        {
            transform.position = target.transform.position;
            // reset fisica
            if (_rb)
            {
#if UNITY_600_OR_NEWER
                _rb.linearVelocity = Vector2.zero;
#else
                _rb.linearVelocity = Vector2.zero;
#endif
                _rb.angularVelocity = 0f;
            }
        }

        // (facoltativo) orienta il player verso la direzione consigliata dallo spawn
        var face = target ? target.faceDir : Vector2.zero;
        if (face.sqrMagnitude > 0.001f)
        {
            // metti qui eventuale flip sprite / rotazione se ti serve
        }
    }
}
