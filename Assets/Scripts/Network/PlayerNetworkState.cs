using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkState : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        Vector3 spawnPos;
        Quaternion spawnRot;

        if (OwnerClientId == 0)
        {
            // Host player (cyan side)
            spawnPos = new Vector3(3.6f, 1f, -1.3f);
            spawnRot = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            // Client player (red side)
            spawnPos = new Vector3(3.6f, 1f, 8.5f);
            spawnRot = Quaternion.Euler(0f, 180f, 0f); // Look at board from opposite side
        }

        transform.SetPositionAndRotation(spawnPos, spawnRot);
    }
}
