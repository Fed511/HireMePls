using System.Collections.Generic;
using UnityEngine;

public class PaperSpawner2D : MonoBehaviour
{
    [Header("Prefab & Aspetto")]
    public GameObject paperPrefab;                 // prefab con SpriteRenderer + PaperFloat2D
    public Sprite[] overrideSprites;               // opzionale: se vuoi randomizzare sprite
    public int orderInLayer = 2;

    [Header("Dove spawna")]
    public Vector2 spawnOffset = new Vector2(0f, 0.5f); // rispetto alla fontana
    public Vector2 spawnSpread = new Vector2(0.3f, 0.2f); // jitter casuale locale (x,y)

    [Header("Tempistica")]
    public Vector2 intervalRange = new Vector2(1.2f, 3.0f); // intervallo casuale tra spawn
    public bool burstSometimes = true;
    public Vector2 burstCountRange = new Vector2(2, 4); // quanti fogli in una raffica
    public float burstProbability = 0.25f;

    [Header("Pool")]
    public int initialPool = 12;
    public int maxPool = 32;

    float _nextSpawnAt;
    readonly Stack<GameObject> _pool = new Stack<GameObject>();
    readonly List<GameObject> _active = new List<GameObject>();

    void Awake()
    {
        if (paperPrefab == null)
        {
            Debug.LogError($"{name}: PaperSpawner2D senza prefab assegnato.");
            enabled = false; return;
        }
        // Pre-pool
        for (int i = 0; i < initialPool; i++) _pool.Push(InstantiateOne());
        ScheduleNextSpawn(Time.time);
    }

    GameObject InstantiateOne()
    {
        var go = Instantiate(paperPrefab);
        go.SetActive(false);
        // Sorting di default
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr)
        {
            sr.sortingOrder = orderInLayer;
            Debug.Log(orderInLayer);
        }
        // Collega callback di "return to pool"
        var pf = go.GetComponent<PaperFloat2D>();
        if (pf) pf.onFinished = ReturnToPool;
        return go;
    }

    void ScheduleNextSpawn(float now)
    {
        float t = Random.Range(intervalRange.x, intervalRange.y);
        _nextSpawnAt = now + Mathf.Max(0.05f, t);
    }

    void Update()
    {
        float now = Time.time;
        if (now < _nextSpawnAt) return;

        // Decidi se fare singolo o burst
        int count = 1;
        if (burstSometimes && Random.value < burstProbability)
            count = Mathf.RoundToInt(Random.Range(burstCountRange.x, burstCountRange.y + 0.999f));

        for (int i = 0; i < count; i++) SpawnOne();

        ScheduleNextSpawn(now);
    }

    void SpawnOne()
    {
        GameObject go = (_pool.Count > 0) ? _pool.Pop() :
                        (_active.Count + _pool.Count < maxPool ? InstantiateOne() : null);
        if (go == null) return;

        // Posizione locale jitterata
        Vector2 jitter = new Vector2(
            Random.Range(-spawnSpread.x, spawnSpread.x),
            Random.Range(-spawnSpread.y, spawnSpread.y)
        );
        Vector3 worldPos = transform.TransformPoint((Vector3)(spawnOffset + jitter));

        // Sprite casuale (se forniti)
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr && overrideSprites != null && overrideSprites.Length > 0)
            sr.sprite = overrideSprites[Random.Range(0, overrideSprites.Length)];

        // Setup e attiva
        go.transform.position = worldPos;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var pf = go.GetComponent<PaperFloat2D>();
        if (pf) pf.Reinitialize();

        go.SetActive(true);
        _active.Add(go);
    }

    void ReturnToPool(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        _active.Remove(go);
        _pool.Push(go);
    }

    // (facoltativo) editor gizmo per area di spawn
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
        Vector3 c = transform.TransformPoint((Vector3)spawnOffset);
        Gizmos.DrawCube(c, new Vector3(spawnSpread.x * 2f, spawnSpread.y * 2f, 0.01f));
    }
}
