using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Core;

public class MultiplayerUI : MonoBehaviour
{
    public static MultiplayerUI Instance;

    [Header("UI References")]
    public GameObject networkMenuPanel;
    public TMP_Text joinCodeText;
    public TMP_InputField joinCodeInput;
    public Button hostButton;
    public Button joinButton;
    [SerializeField] private GameObject lobbyPanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private async void Start()
    {
        hostButton.onClick.AddListener(() => _ = HostGame());
        joinButton.onClick.AddListener(() => _ = JoinGameViaCode(joinCodeInput.text));
        await LobbyManager.Instance.TryAutoMatchLobbyAsync();
    }

    private void SetButtonsInteractable(bool state)
    {
        hostButton.interactable = state;
        joinButton.interactable = state;
    }

    public void UpdateJoinCodeUI(string newCode)
    {
        joinCodeText.text = $"🔗 Join Code: {newCode}";
    }

    public void OnClientConnectedConfirmed()
    {
        Debug.Log("✅ MultiplayerUI: Client fully connected and registered.");
        joinCodeText.text = "Join Success ✔";
    }

    public async Task HostGame()
    {
        SetButtonsInteractable(false);
        joinCodeText.text = "Creating lobby...";

        try
        {
            await UnityServicesManager.InitUnityServicesIfNeeded();

            string joinCode = await LobbyManager.Instance.HostGameFromUI();

            if (!string.IsNullOrEmpty(joinCode))
                joinCodeText.text = $"Join Code: {joinCode}";
            else
                joinCodeText.text = "Failed to create lobby.";
        }
        catch (System.Exception ex)
        {
            joinCodeText.text = "Error creating lobby.";
            Debug.LogError("❌ Host failed: " + ex.Message);
        }

        SetButtonsInteractable(true);
    }

    public async Task JoinGameViaCode(string joinCode)
    {
        SetButtonsInteractable(false);

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            joinCodeText.text = "❌ Invalid join code.";
            SetButtonsInteractable(true);
            return;
        }

        joinCodeText.text = "🔗 Joining...";

        try
        {
            await UnityServicesManager.InitUnityServicesIfNeeded();

            bool relaySuccess = await LobbyManager.Instance.JoinGameFromUI(joinCode);

            if (!relaySuccess)
            {
                joinCodeText.text = "❌ Relay join failed.";
                SetButtonsInteractable(true);
                return;
            }

            joinCodeText.text = "⏳ Waiting for player to spawn...";

            float timeout = 20f;
            float timer = 0f;
            ulong localClientId = NetworkManager.Singleton.LocalClientId;

            while (!PlayerNetworkState.AllPlayers.ContainsKey(localClientId) && timer < timeout)
            {
                await Task.Delay(100);
                timer += 0.1f;
            }

            if (PlayerNetworkState.AllPlayers.TryGetValue(localClientId, out var player))
            {
                PlayerNetworkState.SetLocalPlayer(player);
                joinCodeText.text = "✅ Join Success!";
                Debug.Log($"✅ Local player registered: {localClientId}");
            }
            else
            {
                joinCodeText.text = "⛔ Player spawn timeout.";
                Debug.LogError("❌ Client connected but PlayerNetworkState never registered.");
            }
        }
        catch (System.Exception ex)
        {
            joinCodeText.text = "❌ Error joining lobby.";
            Debug.LogError($"[MultiplayerUI] JoinGameViaCode failed: {ex.Message}");
        }

        SetButtonsInteractable(true);
    }
    public void ShowLobbyPanel()
    {
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
            Debug.Log("📺 Lobby panel shown.");
        }
    }
}
