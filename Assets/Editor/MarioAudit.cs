using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Chan doan: script missing trong prefab/scene, trang thai pause, Error Pause
public static class MarioAudit
{
    [MenuItem("Tools/Mario/Audit")]
    public static void Audit()
    {
        Debug.Log("[Audit] isPlaying=" + EditorApplication.isPlaying +
                  " isPaused=" + EditorApplication.isPaused +
                  " willChange=" + EditorApplication.isPlayingOrWillChangePlaymode +
                  " isCompiling=" + EditorApplication.isCompiling);

        // Tat Error Pause cua Console (neu dang bat)
        var cw = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
        if (cw != null)
        {
            var m = cw.GetMethod("SetConsoleErrorPause", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (m != null)
            {
                m.Invoke(null, new object[] { false });
                Debug.Log("[Audit] Da tat Error Pause");
            }
            else
            {
                Debug.Log("[Audit] Khong tim thay SetConsoleErrorPause");
            }
        }

        // Quet missing script trong cac prefab
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            int count = 0;
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            Debug.Log("[Audit] " + path + " missing=" + count);
        }

        // Quet missing script cac object trong scene dang mo
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            int count = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            if (count > 0) Debug.Log("[Audit] SCENE " + root.name + " missing=" + count);
        }

        Debug.Log("[Audit] Hoan tat");
    }
}
