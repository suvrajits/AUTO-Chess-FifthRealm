using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchStartManager : NetworkBehaviour
{
    public static MatchStartManager Instance;

    [SerializeField] private GameObject botPlayerPrefab;
    [SerializeField] private Transform botSpawnAnchor;

    private void Awake()
    {
        Instance = this;
    }

    public void StartMatch()
    {
        Debug.Log("🚀 Starting multiplayer match...");
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }

    public void StartMatchWithBot()
    {
        Debug.Log("🤖 Injecting bot and starting match...");

        GameObject bot = Instantiate(botPlayerPrefab, botSpawnAnchor.position, Quaternion.identity);
        bot.GetComponent<NetworkObject>().Spawn();

        StartMatch();
    }
}
