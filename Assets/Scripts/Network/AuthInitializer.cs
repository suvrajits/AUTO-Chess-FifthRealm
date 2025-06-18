using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;
using System.Threading.Tasks;

public class AuthInitializer : MonoBehaviour
{
    async void Start()
    {
        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                // Fallback for old SDK: Sign in anonymously
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                Debug.Log("Signed in anonymously. Player ID: " + AuthenticationService.Instance.PlayerId);
            }
            else
            {
                Debug.Log("Already signed in. Player ID: " + AuthenticationService.Instance.PlayerId);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Authentication failed: " + e.Message);
        }
    }
}
