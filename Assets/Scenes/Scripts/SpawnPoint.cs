using UnityEngine;
using UnityEditor;


[ExecuteAlways]
public class SpawnPoint2D : MonoBehaviour
{
    [Header("ID & Facing")]
    public string id = "Default";
    [Tooltip("Direzione consigliata verso cui guarderà il player all’arrivo")]
    public Vector2 faceDir = Vector2.right;

    [Header("Gizmos")]
    public Color gizmoColor = new Color(0.2f, 1f, 0.4f, 0.9f);
    public float radius = 0.12f;        // cerchietto
    public float arrowLength = 0.6f;    // freccia
    public bool showLabel = true;       // mostra l'ID come testo
    public bool onlyWhenSelected = false;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (onlyWhenSelected) return;
        DrawGizmo(false);
    }

    void OnDrawGizmosSelected()
    {
        DrawGizmo(true);
    }

    void DrawGizmo(bool selected)
    {
        // se onlyWhenSelected è true, disegna solo quando è selezionato
        if (onlyWhenSelected && !selected) return;

        // colore principale
        var c = gizmoColor;
        Gizmos.color = c;

        // punto/cerchio
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.DrawWireSphere(transform.position, radius * 1.8f);

        // freccia direzione
        var dir = (faceDir.sqrMagnitude < 0.0001f ? Vector2.right : faceDir.normalized);
        var start = transform.position;
        var end = start + (Vector3)(dir * arrowLength);

        // linea
        Gizmos.DrawLine(start, end);

        // punta freccia (due segmenti)
        var perp = new Vector2(-dir.y, dir.x);
        float head = Mathf.Max(arrowLength * 0.25f, 0.12f);
        Vector3 a = end - (Vector3)(dir * head) + (Vector3)(perp * head * 0.5f);
        Vector3 b = end - (Vector3)(dir * head) - (Vector3)(perp * head * 0.5f);
        Gizmos.DrawLine(end, a);
        Gizmos.DrawLine(end, b);

        if (showLabel)
        {
            Handles.color = Color.white;
            var style = new GUIStyle(EditorStyles.boldLabel) 
            {
                alignment = TextAnchor.LowerCenter
            };
            style.normal.textColor = Color.white;

            Handles.Label(
                transform.position + new Vector3(0, radius * 3f, 0),
                $"Spawn: {id}",
                style
            );
        }
    }
#endif
}
