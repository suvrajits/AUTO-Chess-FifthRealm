using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Loads a scene asynchronously by name.
    /// </summary>
    public async void LoadSceneAsync(string sceneName)
    {
        Debug.Log($"[SceneTransitionManager] Loading scene: {sceneName}...");
        var loadOp = SceneManager.LoadSceneAsync(sceneName);

        while (!loadOp.isDone)
        {
            await Task.Yield();
        }

        Debug.Log($"[SceneTransitionManager] Scene loaded: {sceneName}");
    }
}
