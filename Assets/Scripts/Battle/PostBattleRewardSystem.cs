using UnityEngine;

public class PostBattleRewardSystem : MonoBehaviour
{
    public static PostBattleRewardSystem Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: persist across scenes
    }

    public void GrantGold()
    {
        Debug.Log("Granting post-battle rewards...");

        // Placeholder logic: give 10 gold to every surviving unit's owner
        // You can later replace this with real economy integration
    }
}
