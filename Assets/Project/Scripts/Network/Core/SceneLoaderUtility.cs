using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoaderUtility
{
    public static bool IsSceneLoaded(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        return scene.isLoaded;
    }

    public static IEnumerator EnsureSceneLoaded(string sceneName)
    {
        if (IsSceneLoaded(sceneName))
            yield break;

        Debug.Log($"[SceneLoaderUtility] Loading scene additively: {sceneName}");

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return loadOp;

        Scene scene = SceneManager.GetSceneByName(sceneName);
        yield return new WaitUntil(() => scene.isLoaded);

        Debug.Log($"[SceneLoaderUtility] Scene {sceneName} is now loaded.");
    }

    public static Scene GetLoadedScene(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        return scene.isLoaded ? scene : default;
    }
}
