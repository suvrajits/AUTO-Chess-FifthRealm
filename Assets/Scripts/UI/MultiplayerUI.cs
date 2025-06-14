using Unity.Netcode;
using UnityEngine;

public class MultiplayerUI : MonoBehaviour
{
    public GameObject networkMenuPanel;
    public void Host()
    {
        NetworkManager.Singleton.StartHost();
        networkMenuPanel.SetActive(false);
    }

    public void Join()
    {
        NetworkManager.Singleton.StartClient();
        networkMenuPanel.SetActive(false);
    }
}
