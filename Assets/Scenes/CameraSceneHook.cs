using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class CameraSceneHook : MonoBehaviour
{
    [Header("Target player")]
    public string playerTag = "Player";   // o assegnalo da Inspector

    [Header("Confiner 2D")]
    public PolygonCollider2D confinerCollider; // metti qui il collider della mappa
    public float orthoSize = 5f;               // opzionale: zoom per questa scena

    IEnumerator Start()
    {
        // aspetta un frame per sicurezza (spawn player/rig)
        yield return null;

        var rig = PersistentCameraRig.I;
        if (rig == null || rig.gameplayVCam == null)
        {
            Debug.LogWarning("PersistentCameraRig mancante in questa run.");
            yield break;
        }

        // Player
        Transform player = null;
        if (!string.IsNullOrEmpty(playerTag))
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) player = go.transform;
        }

        // Follow/LookAt
        var vcam = rig.gameplayVCam;
        vcam.Follow = player;
        vcam.LookAt = null; 

        // Ortho size per scena (se vuoi controllare lo zoom scena per scena)
        var cam = rig.mainCamera;
        if (cam && cam.orthographic) cam.orthographicSize = orthoSize;

        // Confiner 2D (estensione della VCam)
        var conf2D = vcam.GetComponent<CinemachineConfiner2D>();
        if (conf2D && confinerCollider)
        {
            conf2D.BoundingShape2D = confinerCollider;
            // importantissimo: invalidare la cache dopo aver assegnato il collider
            conf2D.InvalidateBoundingShapeCache();
            // alcuni setup richiedono un frame in più
            yield return null;
            conf2D.InvalidateBoundingShapeCache();
        }
    }
}
