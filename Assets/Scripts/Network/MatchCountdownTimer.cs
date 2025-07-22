using UnityEngine;
using Unity.Netcode;
using TMPro;

public class MatchCountdownTimer : NetworkBehaviour
{
    public static MatchCountdownTimer Instance;

    [SerializeField] private TMP_Text countdownText; // Optional for UI
    [SerializeField] private float countdownDuration = 60f;

    private NetworkVariable<float> countdownTime = new NetworkVariable<float>(
        value: 0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool hasMatchStarted = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            countdownTime.Value = countdownDuration;
            Debug.Log("⏱️ Match countdown started (60s).");
        }
    }

    public void StartCountdown()
    {
        if (!IsServer) return;

        Debug.Log("⏱️ Host initiating 60-second countdown.");
        countdownTime.Value = countdownDuration;
        hasMatchStarted = false;

        if (countdownText != null)
            countdownText.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (IsServer && !hasMatchStarted)
        {
            HandleCountdownLogic();
        }

        if (IsClient || IsHost)
        {
            UpdateCountdownUI();
        }
    }

    private void HandleCountdownLogic()
    {
        if (countdownTime.Value <= 0f || hasMatchStarted)
            return;

        countdownTime.Value -= Time.deltaTime;
        countdownTime.Value = Mathf.Max(0f, countdownTime.Value);

        int currentPlayers = NetworkManager.Singleton.ConnectedClients.Count;

        if (countdownTime.Value <= 0f || currentPlayers >= 4)
        {
            Debug.Log($"🎮 Triggering match start. TimeLeft={countdownTime.Value:F1}s, Players={currentPlayers}");
            hasMatchStarted = true;

            if (MatchStartManager.Instance != null)
                MatchStartManager.Instance.StartMatch();
            else
                Debug.LogError("❌ MatchStartManager.Instance not found.");
        }
    }

   private void UpdateCountdownUI()
    {
        if (countdownText == null) return;

        int secondsRemaining = Mathf.CeilToInt(countdownTime.Value);
        int currentPlayers = 1; // fallback default

        if (IsServer && NetworkManager.Singleton != null)
        {
            currentPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;
        }

        countdownText.text = $"Match starts in {secondsRemaining}s... ({currentPlayers}/4)";
    }

}
