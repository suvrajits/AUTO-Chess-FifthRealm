using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Netcode;

public class VictoryUIManager : NetworkBehaviour
{
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button playAgainButton;

    private void Awake()
    {
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
        playAgainButton.onClick.AddListener(OnPlayAgainClicked);
    }

    public void ShowResult(string result)
    {
        resultText.text = result;
        panelGroup.alpha = 1f;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;
    }

    private void OnPlayAgainClicked()
    {
        // Only host sends the restart signal to all clients
        if (IsHost)
        {
            RequestRestartGameServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRestartGameServerRpc()
    {
        RestartGameClientRpc();
    }

    [ClientRpc]
    private void RestartGameClientRpc()
    {
        StartCoroutine(RestartGameCoroutine());
    }

    private IEnumerator RestartGameCoroutine()
    {
        // Clean shutdown for all clients/host
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }

        yield return new WaitForSeconds(0.2f); // let shutdown complete

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
