using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using System.Threading.Tasks;

public static class UnityServicesManager
{
    private static bool isInitialized = false;
    private static Task initTask;

    public static async Task InitUnityServicesIfNeeded()
    {
        if (isInitialized)
        {
            Debug.Log("[UnityServices] Already initialized.");
            return;
        }

        if (initTask != null)
        {
            // Await the ongoing initialization
            Debug.Log("[UnityServices] Waiting for concurrent initialization...");
            await initTask;
            return;
        }

        initTask = InitializeServicesInternal();
        await initTask;
    }

    private static async Task InitializeServicesInternal()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[UnityServices] Signed in as: {AuthenticationService.Instance.PlayerId}");
            }

            isInitialized = true;
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"[UnityServices] Auth error: {ex.Message}");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"[UnityServices] Request failed: {ex.Message}");
        }
        finally
        {
            initTask = null; // Clear for future init attempts if needed
        }
    }
}
