using UnityEngine;
using Unity.Netcode;

public class CameraFollowPlayerSide : NetworkBehaviour
{
    public Camera playerCamera;

    

    private void Start()
    {
        if (!IsOwner)
        {
            playerCamera.enabled = false;
            return;
        }

        playerCamera.enabled = true;
       
    }
}
