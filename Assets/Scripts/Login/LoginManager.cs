using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using Assets.SimpleSignIn.Google.Scripts;

public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text statusText;
    public Button googleSignInButton;
    public Button guestLoginButton;

    private void Start()
    {
        statusText.text = "Please sign in to continue...";
        SetButtonsInteractable(true);
    }

    public async void OnGoogleSignInClicked()
    {
        statusText.text = "Signing in with Google...";
        SetButtonsInteractable(false);

        try
        {
            var googleAuth = new GoogleAuth();
            UserInfo user = await googleAuth.SignInAsync();

            if (user != null)
            {
                Debug.Log($"✅ Google Sign-In Success: {user.name} ({user.email})");

                PlayerPrefs.SetString("PlayerName", user.name);
                PlayerPrefs.SetString("PlayerEmail", user.email);
                PlayerPrefs.SetString("PlayerPicUrl", user.picture);

                statusText.text = $"Welcome, {user.name}";
                ProceedToInitScene();
            }
            else
            {
                statusText.text = "Sign-in was cancelled.";
                SetButtonsInteractable(true);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Google Sign-In Error: {ex.Message}");
            statusText.text = $"Sign-In Error: {ex.Message}";
            SetButtonsInteractable(true);
        }
    }

    public void OnGuestLoginClicked()
    {
        SetButtonsInteractable(false);
        string guestName = $"Player_{Random.Range(1000, 9999)}";

        PlayerPrefs.SetString("PlayerName", guestName);
        PlayerPrefs.SetString("PlayerEmail", "guest@offline");
        PlayerPrefs.SetString("PlayerPicUrl", "");

        statusText.text = $"Welcome, {guestName}";
        ProceedToInitScene();
    }

    private void ProceedToInitScene()
    {
        // SceneTransitionManager is assumed to be your async scene loader
        SceneTransitionManager.Instance.LoadSceneAsync("InitScene");
    }

    private void SetButtonsInteractable(bool state)
    {
        googleSignInButton.interactable = state;
        guestLoginButton.interactable = state;
    }
}
