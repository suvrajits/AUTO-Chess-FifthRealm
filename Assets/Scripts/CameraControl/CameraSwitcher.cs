using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class CameraSwitcher : MonoBehaviour
{
    private ulong currentTargetId;

    private void Start()
    {
        currentTargetId = NetworkManager.Singleton.LocalClientId;
        ActivateCameraFor(currentTargetId);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CycleNextCamera();
        }
    }

    private void CycleNextCamera()
    {
        var ids = new List<ulong>(PlayerNetworkState.AllPlayerCameras.Keys);
        ids.Sort(); // ensure deterministic order

        int index = ids.IndexOf(currentTargetId);
        int nextIndex = (index + 1) % ids.Count;
        ulong nextId = ids[nextIndex];

        ActivateCameraFor(nextId);
    }

    private void ActivateCameraFor(ulong clientId)
    {
        foreach (var kvp in PlayerNetworkState.AllPlayerCameras)
        {
            bool active = kvp.Key == clientId;
            kvp.Value.enabled = active;
            kvp.Value.gameObject.SetActive(active);
        }

        Debug.Log($"Switched to camera of player {clientId}");
        currentTargetId = clientId;
    }
}
