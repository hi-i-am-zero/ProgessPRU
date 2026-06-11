using System.Linq;
using UnityEditor;
using UnityEngine;

// Chuan bi editor de chay play mode on dinh khi khong co focus
public static class MarioEditorPrep
{
    [MenuItem("Tools/Mario/Prepare Editor")]
    public static void Prepare()
    {
        // Khong throttle khi editor chay nen
        EditorPrefs.SetInt("InteractionMode", 1);

        // Vao play mode nhanh, khong reload domain (tranh ket transition)
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

        // Dong cac cua so setup/utility co the chan editor loop
        foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            var tn = w.GetType().FullName;
            if (tn.Contains("MCPForUnity") || tn.Contains("SetupWizard") || tn.Contains("McpForUnity"))
            {
                Debug.Log("[MarioEditorPrep] Dong cua so: " + tn);
                try { w.Close(); } catch { }
            }
        }

        // Mo Game View
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        Debug.Log("[MarioEditorPrep] Xong. EnterPlayModeOptions=" + EditorSettings.enterPlayModeOptions);
    }
}
