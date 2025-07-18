using UnityEngine;
using Assets.SimpleSignIn.Google.Scripts;
using UnityEngine.SceneManagement;

public class GoogleSignInManager : MonoBehaviour
{
    private GoogleAuth googleAuth;

    private void Start()
    {
        // Automatically uses GoogleAuthSettings.asset internally
        googleAuth = new GoogleAuth();

        // Try to resume session if previously signed in
        googleAuth.TryResume(OnSignIn, OnTokenReceived);
    }

    public void SignIn()
    {
        googleAuth.SignIn(OnSignIn, caching: true);
    }

    public void SignOut()
    {
        googleAuth.SignOut(revokeAccessToken: true);
        Debug.Log("User signed out.");
    }

    private void OnSignIn(bool success, string error, UserInfo user)
    {
        if (success)
        {
            Debug.Log($"✅ Hello, {user.name} ({user.email})");
            PlayerPrefs.SetString("PlayerName", user.name);
            PlayerPrefs.SetString("PlayerEmail", user.email);
            PlayerPrefs.SetString("PlayerPhoto", user.picture);
            SceneManager.LoadScene("NecodeSetup");
        }
        else
        {
            Debug.LogError($"❌ Sign-In failed: {error}");
        }
    }

    private void OnTokenReceived(bool success, string error, TokenResponse token)
    {
        if (success)
        {
            Debug.Log($"Access Token: {token.AccessToken}");
            Debug.Log($"ID Token (JWT): {token.IdToken}");
        }
        else
        {
            Debug.LogError($"Token error: {error}");
        }
    }
}
