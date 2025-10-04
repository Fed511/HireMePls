using UnityEngine;
using UnityEngine.SceneManagement;

public class Parallax2D : MonoBehaviour
{
    [Header("Camera di riferimento")]
    public Transform cam;                     // Lascia vuoto: prenderà Camera.main
    public bool recenterOnStart = true;       // Ricalibra all'avvio
    public bool recenterOnSceneLoad = true;   // Ricalibra al cambio scena

    [Header("Quanto segue la camera (0 = lontano, 1 = attaccato)")]
    public Vector2 followFactor = new Vector2(0.5f, 0.3f);

    [Header("Blocchi opzionali")]
    public bool lockZ = true;
    public bool ignoreY = false;

    Vector3 _layerStartPos;
    Vector3 _camOrigin;

    void Awake()
    {
        TryFindCamera();                       // <-- nuovo
        _layerStartPos = transform.position;
        if (cam) _camOrigin = cam.position;

        if (recenterOnSceneLoad)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (recenterOnStart && cam) _camOrigin = cam.position;
    }

    void OnDestroy()
    {
        if (recenterOnSceneLoad)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        TryFindCamera();                       // <-- ri-aggancia anche dopo load
        if (cam) _camOrigin = cam.position;
        _layerStartPos = transform.position;
    }

    void LateUpdate()
    {
        if (!cam)
        {
            // se la camera non è ancora pronta (ordine di inizializzazione), riprova
            TryFindCamera();
            if (!cam) return;
        }

        Vector3 delta = cam.position - _camOrigin;

        float fx = followFactor.x;
        float fy = ignoreY ? 0f : followFactor.y;

        Vector3 p = _layerStartPos + new Vector3(delta.x * fx, delta.y * fy, 0f);
        if (lockZ) p.z = _layerStartPos.z;

        transform.position = p;
    }

    void TryFindCamera()
    {
        if (cam) return;

        // 1) prova Camera.main (richiede che la Main Camera abbia il tag "MainCamera")
        var main = Camera.main;
        if (main) { cam = main.transform; return; }

        // 2) fallback: qualunque Camera attiva
        var anyCam = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);
        if (anyCam) { cam = anyCam.transform; return; }
    }

    [ContextMenu("Recenter Now")]
    void RecenterNow()
    {
        TryFindCamera();
        if (cam) _camOrigin = cam.position;
        _layerStartPos = transform.position;
    }
}
