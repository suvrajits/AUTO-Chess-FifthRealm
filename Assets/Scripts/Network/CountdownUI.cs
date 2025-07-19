using TMPro;
using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class CountdownUI : MonoBehaviour
{
    public static CountdownUI Instance;

    [SerializeField] private TMP_Text countdownText;

    private void Awake()
    {
        Instance = this;
    }

    public void BindCountdown(NetworkVariable<float> countdownTime)
    {
        StartCoroutine(UpdateCountdown(countdownTime));
    }

    private IEnumerator UpdateCountdown(NetworkVariable<float> countdownTime)
    {
        while (true)
        {
            countdownText.text = $"Match starting in {Mathf.CeilToInt(countdownTime.Value)}s...";
            yield return new WaitForSeconds(0.5f);
        }
    }
}
