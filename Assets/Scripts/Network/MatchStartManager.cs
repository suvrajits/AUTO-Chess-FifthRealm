using UnityEngine;
using Unity.Netcode;

public class MatchStartManager : NetworkBehaviour
{
    public static MatchStartManager Instance;

    [Header("UI Panels")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject heroSelectionPanel;

    [Header("Prefabs")]
    [SerializeField] private GameObject gameGridPrefab;
    public static event System.Action OnMatchStarted;

    private bool hasMatchStarted = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Can be called by CountdownTimer, Host UI button, or auto-start logic.
    /// Handles hiding lobby, showing selection, and spawning grid.
    /// </summary>
    public void StartMatch()
    {
        if (hasMatchStarted)
        {
            Debug.LogWarning("⚠️ MatchStartManager: Match already started. Ignoring duplicate trigger.");
            return;
        }

        if (!IsServer)
        {
            Debug.LogWarning("⚠️ Only the server can start the match.");
            return;
        }

        Debug.Log("🚀 Starting match...");
        hasMatchStarted = true;

        // 🔒 UI transitions on host
        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        if (heroSelectionPanel != null)
            heroSelectionPanel.SetActive(true);

        // 🌐 Propagate UI transition to all clients
        HideLobbyPanelClientRpc();

        // 🧱 Spawn the game grid prefab
        if (gameGridPrefab != null)
        {
            GameObject grid = Instantiate(gameGridPrefab);
            var netObj = grid.GetComponent<NetworkObject>();

            if (netObj != null)
            {
                netObj.Spawn();
                Debug.Log("✅ gameGridPrefab spawned and networked.");
            }
            else
            {
                Debug.LogError("❌ gameGridPrefab is missing a NetworkObject component.");
            }
        }
        else
        {
            Debug.LogError("❌ gameGridPrefab not assigned in inspector.");
        }

        // 🎮 Trigger phase controller and signal listeners
        PhaseController.Instance?.StartPhaseLoop();
        OnMatchStarted?.Invoke();
    }


    [ClientRpc]
    private void HideLobbyPanelClientRpc()
    {
        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        if (heroSelectionPanel != null)
            heroSelectionPanel.SetActive(true);
    }
    public void ResetMatch()
    {
        hasMatchStarted = false;
        Debug.Log("🔁 MatchStartManager: Match state reset.");
    }
}
