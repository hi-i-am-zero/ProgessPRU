using UnityEditor;
using UnityEngine;

// Bom frame cho play mode khi editor chay nen khong duoc cap frame
public static class MarioStepper
{
    [MenuItem("Tools/Mario/Step 300")]
    public static void Step300()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.Log("[Stepper] Khong o play mode");
            return;
        }
        for (int i = 0; i < 300; i++)
            EditorApplication.Step();
        Debug.Log("[Stepper] Da step 300 frame. time=" + Time.time.ToString("F2"));
    }

    [MenuItem("Tools/Mario/Resume")]
    public static void Resume()
    {
        EditorApplication.isPaused = false;
        Debug.Log("[Stepper] Resume");
    }
}
