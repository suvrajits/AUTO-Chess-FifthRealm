using Unity.Netcode;
using UnityEngine;

public class PlayerStatus : NetworkBehaviour
{
    public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LobbyManager.Instance.RegisterLocalPlayer(this);
        }

        IsReady.OnValueChanged += OnReadyStateChanged;
    }

    private void OnReadyStateChanged(bool oldValue, bool newValue)
    {
        LobbyManager.Instance.UpdatePlayerReadyState(this, newValue);
    }

    [ServerRpc(RequireOwnership = true)]
    public void SetReadyServerRpc(bool ready)
    {
        IsReady.Value = ready;
    }
}
