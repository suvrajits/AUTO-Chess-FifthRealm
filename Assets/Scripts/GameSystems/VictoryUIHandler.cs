using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI; // For Button, if needed

public class VictoryUIHandler : MonoBehaviour
{
    public static VictoryUIHandler Instance;

    [Header("UI References")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private GameObject eliminatedPanel;

    [SerializeField] private Button restartButton; // optional if assigned via inspector

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        HideAll();

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
    }

    public void ShowResult(bool isWinner)
    {
        HideAll();

        if (isWinner)
        {
            victoryPanel.SetActive(true);
        }
        else
        {
            defeatPanel.SetActive(true);
        }

        // Only host can restart game
        if (restartButton != null)
            restartButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);
    }

    public void ShowEliminated()
    {
        HideAll();
        eliminatedPanel.SetActive(true);

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);
    }

    private void HideAll()
    {
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        eliminatedPanel.SetActive(false);

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);
    }

    private void OnRestartClicked()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        Debug.Log("🔁 Restarting game...");

        // Clean shutdown, then reload
        NetworkManager.Singleton.Shutdown();

        // Optional: clear static references
        PlayerNetworkState.AllPlayers.Clear();
        PlayerNetworkState.AllPlayerCameras.Clear();

        // Reload scene (assuming it's index 0 or name-based)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
