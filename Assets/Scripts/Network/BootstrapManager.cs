using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class BootstrapManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject networkManagerPrefab;
    [SerializeField] private GameObject shopManagerPrefab;

    private static bool isNetworkInitialized = false;
    private static bool shopManagerSpawned = false;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("🚀 Scene reloaded. Rebooting services...");

        await UnityServicesManager.InitUnityServicesIfNeeded();

        // ✅ Spawn NetworkManager only if missing
        if (NetworkManager.Singleton == null)
        {
            Instantiate(networkManagerPrefab);
            Debug.Log("📡 NetworkManager instantiated.");
        }

        if (!isNetworkInitialized)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            isNetworkInitialized = true;
        }
    }

    private void OnServerStarted()
    {
        if (!shopManagerSpawned)
        {
            SpawnShopManager();
        }
    }

    private void SpawnShopManager()
    {
        if (shopManagerPrefab == null)
        {
            Debug.LogError("❌ shopManagerPrefab is not assigned in BootstrapManager!");
            return;
        }

        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("🚫 ShopManager should only be spawned by the server.");
            return;
        }

        GameObject obj = Instantiate(shopManagerPrefab);
        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
            Debug.Log("🛒 ShopManager spawned and registered as NetworkObject.");
        }
        else
        {
            Debug.LogWarning("⚠️ ShopManager prefab missing NetworkObject component.");
        }

        shopManagerSpawned = true;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }
}
