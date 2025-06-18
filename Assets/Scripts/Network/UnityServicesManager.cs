using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using UnityEngine;

public class UnityServicesManager : MonoBehaviour
{
    public static bool IsInitialized = false;

    private async void Awake()
    {
        await InitUnityServicesIfNeeded();
    }

    public static async Task InitUnityServicesIfNeeded()
    {
        if (IsInitialized) return;

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        IsInitialized = true;
        Debug.Log("[UnityServices] Initialized and signed in anonymously.");
    }
}
