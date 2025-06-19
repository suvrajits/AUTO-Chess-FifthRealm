using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CameraSwitcherUI : MonoBehaviour
{
    [SerializeField] private Button spectateButton;

    private ulong currentTargetId;
    public static ulong CurrentTargetId { get; private set; }
    private void Start()
    {
        StartCoroutine(WaitForCamerasThenActivate());
    }

    private IEnumerator WaitForCamerasThenActivate()
    {
        // Wait until the local player and others register their cameras
        while (PlayerNetworkState.AllPlayerCameras.Count == 0)
            yield return null;

        currentTargetId = NetworkManager.Singleton.LocalClientId;

        // Bind button after cameras are ready
        if (spectateButton == null)
        {
            Debug.LogError("❌ Spectate Button not assigned in inspector.");
            yield break;
        }

        spectateButton.onClick.AddListener(OnSpectateClicked);

        // Activate local player's camera first
        ActivateCamera(currentTargetId);
    }

    private void OnSpectateClicked()
    {
        var ids = PlayerNetworkState.AllPlayerCameras.Keys.OrderBy(id => id).ToList();
        if (ids.Count == 0) return;

        int index = ids.IndexOf(currentTargetId);
        int nextIndex = (index + 1) % ids.Count;
        currentTargetId = ids[nextIndex];

        ActivateCamera(currentTargetId);
    }

    private void ActivateCamera(ulong clientId)
    {
        CurrentTargetId = clientId;

        foreach (var kvp in PlayerNetworkState.AllPlayerCameras)
        {
            bool active = kvp.Key == clientId;
            if (kvp.Value != null)
            {
                kvp.Value.enabled = active;
                kvp.Value.gameObject.SetActive(active);
            }
        }

        Debug.Log($"📷 Switched to Player {clientId}");
    }
}
