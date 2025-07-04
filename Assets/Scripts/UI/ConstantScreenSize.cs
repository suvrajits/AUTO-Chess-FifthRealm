using UnityEngine;

[ExecuteAlways]
public class ConstantScreenSize : MonoBehaviour
{
    public float targetScreenHeight = 100f;
    public Camera mainCam;

    private void LateUpdate()
    {
        if (mainCam == null || targetScreenHeight <= 0f)
            return;

        float distance = Vector3.Distance(transform.position, mainCam.transform.position);
        float scaleFactor = distance / targetScreenHeight;

        transform.localScale = Vector3.one * scaleFactor;
    }

    public void SetCamera(Camera cam)
    {
        mainCam = cam;
    }
}
