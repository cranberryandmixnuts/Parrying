using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissingScriptScanner
{
    [MenuItem("Tools/Missing Scripts/Scan Loaded Scenes")]
    public static void ScanLoadedScenes()
    {
        int totalMissingCount = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++)
            totalMissingCount += ScanScene(SceneManager.GetSceneAt(i));

        Debug.Log($"[Missing Script Scan] Loaded scenes scan finished. Found {totalMissingCount} missing component(s).");
    }

    [MenuItem("Tools/Missing Scripts/Scan Entire Project")]
    public static void ScanEntireProject()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        SceneSetup[] sceneSetup = EditorSceneManager.GetSceneManagerSetup();
        int totalMissingCount = 0;

        try
        {
            totalMissingCount += ScanAllScenesInAssets();
            totalMissingCount += ScanAllPrefabsInAssets();
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
        }

        Debug.Log($"[Missing Script Scan] Project scan finished. Found {totalMissingCount} missing component(s).");
    }

    public static int ScanAllScenesInAssets()
    {
        int totalMissingCount = 0;
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });

        foreach (string sceneGuid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            totalMissingCount += ScanScene(scene);
        }

        return totalMissingCount;
    }

    public static int ScanAllPrefabsInAssets()
    {
        int totalMissingCount = 0;
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

        foreach (string prefabGuid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                foreach (Transform child in prefabRoot.GetComponentsInChildren<Transform>(true))
                {
                    GameObject gameObject = child.gameObject;
                    int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);

                    if (missingCount <= 0)
                        continue;

                    totalMissingCount += missingCount;
                    Debug.LogError($"[Missing Script][Prefab] {prefabPath} | {GetHierarchyPath(gameObject)} | Missing: {missingCount}", prefabAsset);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        return totalMissingCount;
    }

    public static int ScanScene(Scene scene)
    {
        int totalMissingCount = 0;
        GameObject[] roots = scene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject gameObject = child.gameObject;
                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);

                if (missingCount <= 0)
                    continue;

                totalMissingCount += missingCount;
                Debug.LogError($"[Missing Script][Scene] {scene.path} | {GetHierarchyPath(gameObject)} | Missing: {missingCount}", gameObject);
            }
        }

        return totalMissingCount;
    }

    public static string GetHierarchyPath(GameObject gameObject)
    {
        string path = gameObject.name;
        Transform current = gameObject.transform.parent;

        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
    }
}