using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using UnityEngine;

public class UnityServicesManager : MonoBehaviour
{
    public static bool IsInitialized { get; private set; } = false;
    public static UnityServicesManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _ = InitUnityServicesIfNeeded();
    }

    public static async Task InitUnityServicesIfNeeded()
    {
        if (IsInitialized) return;

        try
        {
            Debug.Log("[UnityServices] Initializing...");

            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[UnityServices] Signed in as anonymous user: {AuthenticationService.Instance.PlayerId}");
            }

            IsInitialized = true;
            Debug.Log("[UnityServices]  Initialization complete.");
        }
        catch (AuthenticationException authEx)
        {
            Debug.LogError($"[UnityServices]  Auth Error: {authEx.Message}");
        }
        catch (ServicesInitializationException initEx)
        {
            Debug.LogError($"[UnityServices]  Initialization Error: {initEx.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UnityServices]  Unknown Error: {ex.Message}");
        }
    }
}
