using System.Collections.Generic;
using UnityEngine;

public class GhostSpawner2D : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject ghostPrefab; // contiene SpriteRenderer + GhostMover2D

    [Header("Timing")]
    public Vector2 intervalRange = new Vector2(2f, 5f); // intervallo casuale

    [Header("Spawn & Kill bounds")]
    public float spawnMarginRight = 1.0f;   // quanto oltre il bordo destro spawna
    public float killMarginLeft = 0.5f;     // margine extra prima del despawn
    public Vector2 yWorldRange = new Vector2(-2f, 2f);

    [Header("Parametri random movimento")]
    public Vector2 speedRange = new Vector2(1.6f, 3.2f);
    public Vector2 amplitudeRange = new Vector2(0.15f, 0.35f);
    public Vector2 frequencyRange = new Vector2(1.8f, 3.2f);

    [Header("Lifetime")]
    public Vector2 lifetimeRange = new Vector2(5f, 9f);

    [Header("Sorting (opzionale)")]
    public string sortingLayer = "";
    public int sortingOrder = 0;
    public float startAlpha = 0.9f;

    [Header("Pool")]
    public int initialPool = 8;
    public int maxPool = 24;

    float _nextSpawnAt;
    readonly Stack<GameObject> _pool = new Stack<GameObject>();
    readonly List<GameObject> _active = new List<GameObject>();

    void Awake()
    {
        if (!ghostPrefab)
        {
            Debug.LogError($"{name}: GhostSpawnerPool2D senza prefab.");
            enabled = false; return;
        }
        // Pre-pool
        for (int i = 0; i < initialPool; i++) _pool.Push(InstantiateOne());
    }

    void Start() => ScheduleNext(Time.time);

    void ScheduleNext(float now)
        => _nextSpawnAt = now + Random.Range(intervalRange.x, intervalRange.y);

    GameObject InstantiateOne()
    {
        var go = Instantiate(ghostPrefab);
        go.SetActive(false);

        var mover = go.GetComponent<GhostMover2D>();
        if (mover == null) mover = go.AddComponent<GhostMover2D>();
        mover.onDespawn = ReturnToPool;

        // sorting opzionale
        var sr = go.GetComponentInChildren<SpriteRenderer>(true);
        if (sr)
        {
            if (!string.IsNullOrEmpty(sortingLayer)) sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
            var c = sr.color; c.a = startAlpha; sr.color = c;
        }
        return go;
    }

    void Update()
    {
        if (Time.time >= _nextSpawnAt)
        {
            SpawnOne();
            ScheduleNext(Time.time);
        }
    }

    void SpawnOne()
    {
        var cam = Camera.main;
        if (!cam) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float camX = cam.transform.position.x;

        float leftBound = camX - halfW;
        float rightBound = camX + halfW;

        // posizione di spawn (fuori dallo schermo a destra)
        float startX = rightBound + spawnMarginRight;
        float y = Random.Range(yWorldRange.x, yWorldRange.y);
        Vector3 startPos = new Vector3(startX, y, 0f);

        // prendi dal pool o crea
        GameObject go = (_pool.Count > 0) ? _pool.Pop()
                         : (_active.Count + _pool.Count < maxPool ? InstantiateOne() : null);
        if (!go) return;

        var mover = go.GetComponent<GhostMover2D>();
        var sr = go.GetComponentInChildren<SpriteRenderer>(true);

        // parametri random
        float spd = Random.Range(speedRange.x, speedRange.y);
        float amp = Random.Range(amplitudeRange.x, amplitudeRange.y);
        float frq = Random.Range(frequencyRange.x, frequencyRange.y);
        float life = Random.Range(lifetimeRange.x, lifetimeRange.y);

        // reinit e attiva
        mover.Reinitialize(startPos, spd, amp, frq,
                           leftBound, killMarginLeft,
                           life, startAlpha, faceLeft: true);

        if (sr)
        {
            if (!string.IsNullOrEmpty(sortingLayer)) sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
        }

        go.SetActive(true);
        _active.Add(go);
    }

    void ReturnToPool(GhostMover2D mover)
    {
        if (!mover) return;
        var go = mover.gameObject;
        go.SetActive(false);
        _active.Remove(go);
        _pool.Push(go);
    }

    // Gizmo per range Y
    void OnDrawGizmosSelected()
    {
        var cam = Camera.main;
        if (!cam) return;
        Gizmos.color = new Color(0.7f, 1f, 1f, 0.25f);
        float yMid = (yWorldRange.x + yWorldRange.y) * 0.5f;
        float ySize = Mathf.Abs(yWorldRange.y - yWorldRange.x);
        float x = cam.transform.position.x + cam.orthographicSize * cam.aspect + spawnMarginRight;
        Gizmos.DrawCube(new Vector3(x, yMid, 0f), new Vector3(0.2f, ySize, 0.01f));
    }
}
