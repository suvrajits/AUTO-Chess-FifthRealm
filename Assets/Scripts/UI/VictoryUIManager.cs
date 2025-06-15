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
        HidePanel();
        playAgainButton.onClick.AddListener(OnPlayAgainClicked);
    }

    public void ShowResult(string result)
    {
        resultText.text = result;
        panelGroup.alpha = 1f;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;
    }

    private void HidePanel()
    {
        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
    }

    private void OnPlayAgainClicked()
    {
        HidePanel(); // Hide UI on both players

        // Always let host handle the restart trigger
        if (IsHost)
        {
            RestartGameClientRpc();
        }
        else
        {
            RequestRestartFromHostServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRestartFromHostServerRpc(ServerRpcParams rpcParams = default)
    {
        // Only host should handle the restart
        if (IsHost)
        {
            RestartGameClientRpc();
        }
    }

    [ClientRpc]
    private void RestartGameClientRpc()
    {
        HidePanel(); // Hide panel for safety
        StartCoroutine(RestartGameCoroutine());
    }

    private IEnumerator RestartGameCoroutine()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        yield return new WaitForSeconds(0.2f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
