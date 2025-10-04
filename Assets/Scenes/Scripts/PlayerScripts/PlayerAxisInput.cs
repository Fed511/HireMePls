using UnityEngine;

public class PlayerAxisInput : MonoBehaviour, IInputSource2D
{
    public bool useRaw = true; // Raw = istantaneo, non Raw = smussato
    public string horizontal = "Horizontal";
    public string vertical = "Vertical";

    public Vector2 GetMove()
    {
        if (ControlLock.IsLocked) return Vector2.zero;

        float x = useRaw ? Input.GetAxisRaw(horizontal) : Input.GetAxis(horizontal);
        float y = useRaw ? Input.GetAxisRaw(vertical) : Input.GetAxis(vertical);
        return new Vector2(x, y).normalized;
    }
}
