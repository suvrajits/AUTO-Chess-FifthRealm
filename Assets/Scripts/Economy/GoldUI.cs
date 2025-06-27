using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private GameObject notEnoughGoldWarning;

    private GoldManager goldManager;

    private void Start()
    {
        var localPlayer = PlayerNetworkState.LocalPlayer;
        if (localPlayer == null)
        {
            Debug.LogWarning("🟡 LocalPlayer not ready. Retrying...");
            StartCoroutine(WaitForLocalPlayer());
            return;
        }

        goldManager = localPlayer.GetComponent<GoldManager>();

        if (goldManager != null)
        {
            goldManager.CurrentGold.OnValueChanged += OnGoldChanged;
            UpdateGoldUI(goldManager.CurrentGold.Value);
        }
    }

    private IEnumerator WaitForLocalPlayer()
    {
        yield return new WaitUntil(() => PlayerNetworkState.LocalPlayer != null);
        Start();
    }

    private void OnGoldChanged(int oldValue, int newValue)
    {
        UpdateGoldUI(newValue);
    }

    private void UpdateGoldUI(int gold)
    {
        if (goldText != null)
            goldText.text = $"{gold}";

        // Optional: Hide warning
        if (notEnoughGoldWarning != null)
            notEnoughGoldWarning.SetActive(gold < 2); // 2 is Reroll cost
    }

    private void OnDestroy()
    {
        if (goldManager != null)
            goldManager.CurrentGold.OnValueChanged -= OnGoldChanged;
    }
    public void RerollTest()
    {
        var player = PlayerNetworkState.LocalPlayer;
        if (player != null && player.GoldManager.TrySpendGold(2))
        {
            Debug.Log("🔄Rerolled!");
        }
        else
        {
            Debug.Log("❌ Not enough gold.");
        }
    }

}
