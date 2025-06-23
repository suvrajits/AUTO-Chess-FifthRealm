
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.Netcode;

public class MultiplayerUI : MonoBehaviour
{
    public static MultiplayerUI Instance;

    [Header("UI References")]
    public GameObject networkMenuPanel;
    public TMP_Text joinCodeText;
    public TMP_InputField joinCodeInput;
    public Button hostButton;
    public Button joinButton;

    public void OnClickHost() => _ = HostGame();
    public void OnClickJoin() => _ = JoinGame();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

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

            if (!string.IsNullOrEmpty(joinCode))
                joinCodeText.text = $"Join Code: {joinCode}";
            else
                joinCodeText.text = "Failed to create lobby.";
        }
        catch (System.Exception ex)
        {
            joinCodeText.text = "Error creating lobby.";
            Debug.LogError("Failed to create host: " + ex.Message);
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

            if (success)
            {
                ulong localId = NetworkManager.Singleton.LocalClientId;
                float timeout = 20f;
                float timer = 0f;

                while (!PlayerNetworkState.AllPlayers.ContainsKey(localId) && timer < timeout)
                {
                    await Task.Delay(100);
                    timer += 0.1f;
                }

                if (PlayerNetworkState.AllPlayers.ContainsKey(localId))
                    joinCodeText.text = "Join Success";
                else
                    joinCodeText.text = "Player spawn timeout.";
            }
            else
            {
                joinCodeText.text = "Join failed. Try again.";
            }
        }
        catch (System.Exception ex)
        {
            joinCodeText.text = "Error joining lobby.";
            Debug.LogError("Failed to join: " + ex.Message);
        }

        SetButtonsInteractable(true);
    }

    public void UpdateJoinCodeUI(string newCode)
    {
        joinCodeText.text = $"Join Code: {newCode}";
    }

    private void SetButtonsInteractable(bool state)
    {
        hostButton.interactable = state;
        joinButton.interactable = state;
    }
}
