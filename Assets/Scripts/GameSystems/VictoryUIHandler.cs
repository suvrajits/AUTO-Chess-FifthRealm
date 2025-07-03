using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections;

public class VictoryUIHandler : MonoBehaviour
{
    public static VictoryUIHandler Instance;

    [Header("UI References")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private GameObject eliminatedPanel;
    //[SerializeField] private GameObject restartButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        HideAll();
    }

    public void ShowResult(bool isWinner)
    {
        HideAll();

        if (isWinner)
        {
            victoryPanel.SetActive(true);
            //restartButton.SetActive(NetworkManager.Singleton.IsServer); // Host-only restart
        }
        else
        {
            defeatPanel.SetActive(true);
            // Auto-restart for loser
        }
        StartCoroutine(RestartAfterDelay());
    }

    public void ShowEliminated()
    {
        HideAll();
        eliminatedPanel.SetActive(true);
        StartCoroutine(RestartAfterDelay()); // Auto-restart for eliminated
    }

    private void HideAll()
    {
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        eliminatedPanel.SetActive(false);
        //restartButton?.SetActive(false);
    }

    public void OnRestartButtonPressed()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // Reload for everyone
        NetworkManager.Singleton.SceneManager.LoadScene("YourSceneName", LoadSceneMode.Single);
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        // Leave network and return to main scene for next round
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("NecodeSetup");
    }
}
