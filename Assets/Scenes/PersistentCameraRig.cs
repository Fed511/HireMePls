using UnityEngine;
using Unity.Cinemachine;

[DefaultExecutionOrder(-300)]
public class PersistentCameraRig : MonoBehaviour
{
    public static PersistentCameraRig I { get; private set; }

    [Header("Refs")]
    public Camera mainCamera;
    public CinemachineCamera gameplayVCam;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // fallback se non assegnati in Inspector
        if (!mainCamera) mainCamera = GetComponentInChildren<Camera>(true);
        if (!gameplayVCam) gameplayVCam = GetComponentInChildren<CinemachineCamera>(true);
    }
}
