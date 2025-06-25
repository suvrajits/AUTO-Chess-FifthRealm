using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    private Transform camTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("🎥 BillboardToCamera: No main camera found.");
        }
    }

    void LateUpdate()
    {
        if (camTransform == null) return;

        // Face the camera only on the Y-axis (horizontal rotation only)
        Vector3 lookDirection = camTransform.position - transform.position;
        lookDirection.y = 0f; // Lock vertical axis
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(-lookDirection);
        }
    }
}
