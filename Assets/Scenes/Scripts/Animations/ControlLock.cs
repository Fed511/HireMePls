using UnityEngine;

public static class ControlLock
{
    static int _locks;
    public static bool IsLocked => _locks > 0;

    public static void Push(string reason = null)
    {
        _locks++;
        // Debug.Log($"[ControlLock] +1 ({_locks}) {reason}");
    }

    public static void Pop(string reason = null)
    {
        _locks = Mathf.Max(0, _locks - 1);
        // Debug.Log($"[ControlLock] -1 ({_locks}) {reason}");
    }

    public static void ClearAll() { _locks = 0; }

}
