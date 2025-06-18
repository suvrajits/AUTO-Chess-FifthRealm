using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class BootstrapManager : MonoBehaviour
{
    [SerializeField] private GameObject networkManagerPrefab;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("🚀 Scene reloaded. Rebooting services...");

        await UnityServicesManager.InitUnityServicesIfNeeded();

        // Ensure only one instance
        if (NetworkManager.Singleton == null)
        {
            Instantiate(networkManagerPrefab);
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
