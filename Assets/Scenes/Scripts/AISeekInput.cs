using UnityEngine;

public class AISeekInput : MonoBehaviour, IInputSource2D
{
    public Transform target;
    public float stopDistance = 0f; // se >0, si ferma a distanza
    public float randomJitter = 0f; // piccola casualità per evitare sovrapposizione perfetta

    public Vector2 GetMove()
    {
        if (target == null) return Vector2.zero;
        Vector2 to = (Vector2)(target.position - transform.position);
        float dist = to.magnitude;
        if (dist <= Mathf.Max(0.001f, stopDistance)) return Vector2.zero;

        Vector2 dir = to / dist; // normalizzato
        if (randomJitter > 0f)
            dir += Random.insideUnitCircle * randomJitter;

        return dir.normalized; // valore in [-1..1] come richiesto dal motore
    }
}
