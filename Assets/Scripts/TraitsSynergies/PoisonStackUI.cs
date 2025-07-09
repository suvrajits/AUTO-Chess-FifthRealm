using UnityEngine;
using TMPro;

public class PoisonStackUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stackText;
    private Transform followTarget;

    public void Init(Transform target)
    {
        followTarget = target;
    }

    public void SetStacks(int stackCount)
    {
        if (stackCount <= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        stackText.text = $"x{stackCount}";
    }

    private void Update()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position + Vector3.up * 2.1f; // Adjust if overlapping
            transform.LookAt(Camera.main.transform);
        }
    }
}
