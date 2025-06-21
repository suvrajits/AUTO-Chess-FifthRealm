using UnityEngine;

public class SpawnAnchorRegistry : MonoBehaviour
{
    public static SpawnAnchorRegistry Instance;

    public Transform[] playerAnchors = new Transform[8];

    private void Awake()
    {
        Instance = this;
    }

    public Transform GetAnchor(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < playerAnchors.Length)
            return playerAnchors[playerIndex];
        return null;
    }
}
