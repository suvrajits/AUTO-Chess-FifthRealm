using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("🎥 BillboardToCamera: No main camera found.");
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // Match camera rotation exactly (perfectly parallel to screen)
        transform.rotation = mainCamera.transform.rotation;
    }
}
