using UnityEditor;
using UnityEngine;

// Khi editor chay nen (khong focus), Unity co the khong tick player loop.
// Driver nay ep player loop chay moi editor update de game van hoat dong.
[InitializeOnLoad]
public static class PlayModeDriver
{
    static PlayModeDriver()
    {
        EditorApplication.update += Pump;
    }

    static void Pump()
    {
        if (EditorApplication.isPlaying && !EditorApplication.isPaused && !EditorApplication.isCompiling)
            EditorApplication.QueuePlayerLoopUpdate();
    }
}
