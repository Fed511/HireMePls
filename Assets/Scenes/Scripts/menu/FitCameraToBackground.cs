using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FitCameraToBackground : MonoBehaviour
{
    public SpriteRenderer background;   // assegna il BG
    [Range(0f, 0.5f)] public float padding = 0.0f; // margine extra in unità

    void Start()
    {
        if (background == null) { Debug.LogWarning("Assegna il background!"); return; }
        var cam = GetComponent<Camera>();
        cam.orthographic = true;

        // centra la camera sul BG
        var b = background.bounds;
        transform.position = new Vector3(b.center.x, b.center.y, transform.position.z);

        // calcola la orthographicSize necessaria a contenere tutto il BG
        float halfHeight = b.extents.y + padding;
        float halfWidth = b.extents.x + padding;

        // per riempire tutto, consideriamo anche l'aspect ratio della camera
        float sizeByHeight = halfHeight;
        float sizeByWidth = halfWidth / cam.aspect;

        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }
}
