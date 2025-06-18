using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.Netcode;

public class MultiplayerUI : MonoBehaviour
{

    [Header("UI References")]
    public GameObject networkMenuPanel;
    public TMP_Text joinCodeText;
    public TMP_InputField joinCodeInput;
    public Button hostButton;
    public Button joinButton;

    public void OnClickHost() => _ = HostGame();
    public void OnClickJoin() => _ = JoinGame();


    private void Start()
    {
        hostButton.onClick.AddListener(OnClickHost);
        joinButton.onClick.AddListener(OnClickJoin);
    }
    private async Task HostGame()
    {
        SetButtonsInteractable(false);
        joinCodeText.text = "Creating lobby...";

        try
        {
            string joinCode = await LobbyManager.Instance.HostGameFromUI();

            Debug.Log($"[HostGame] NetworkManager State - IsHost: {NetworkManager.Singleton.IsHost}, IsClient: {NetworkManager.Singleton.IsClient}, IsServer: {NetworkManager.Singleton.IsServer}");

            if (!string.IsNullOrEmpty(joinCode))
            {
                joinCodeText.text = $"Join Code: {joinCode}";
                //if (networkMenuPanel != null) networkMenuPanel.SetActive(false);
            }
            else
            {
                joinCodeText.text = "Failed to create lobby.";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to create host: " + ex.Message);
            joinCodeText.text = "Error creating lobby.";
        }

        SetButtonsInteractable(true);
    }

    private async Task JoinGame()
    {
        SetButtonsInteractable(false);
        string code = joinCodeInput?.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(code))
        {
            joinCodeText.text = "Please enter a valid code.";
            SetButtonsInteractable(true);
            return;
        }

        joinCodeText.text = "Joining lobby...";

        try
        {
            bool success = await LobbyManager.Instance.JoinGameFromUI(code);

            Debug.Log($"[JoinGame] NetworkManager State - IsHost: {NetworkManager.Singleton.IsHost}, IsClient: {NetworkManager.Singleton.IsClient}, IsServer: {NetworkManager.Singleton.IsServer}");

            if (success)
            {
                networkMenuPanel.SetActive(false);
                joinCodeText.text = "Join Success";
            }
            else
            {
                joinCodeText.text = "Join failed. Try again.";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to join: " + ex.Message);
            joinCodeText.text = "Error joining lobby.";
        }

        SetButtonsInteractable(true);
    }
    private void SetButtonsInteractable(bool state)
    {
        if (hostButton != null) hostButton.interactable = state;
        if (joinButton != null) joinButton.interactable = state;
    }
}
