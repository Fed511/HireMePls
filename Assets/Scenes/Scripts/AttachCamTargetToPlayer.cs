using UnityEngine;

public class AttachCamTargetToPlayer : MonoBehaviour
{
    public string playerTag = "Player";
    public Vector3 localOffset = Vector3.zero; // offset dal player (es. (0,0,0))

    void Start()
    {
        var pgo = GameObject.FindGameObjectWithTag(playerTag);
        if (!pgo)
        {
            Debug.LogWarning("[AttachCamTargetToPlayer] Player non trovato.");
            return;
        }

        // attacca CamTarget al player persistente
        transform.SetParent(pgo.transform, worldPositionStays: false);
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;
    }
}
