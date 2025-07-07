using UnityEngine;
using TMPro;
using Unity.Netcode;

public class RoundHUDUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text unitCapText;

    [Header("Flash Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color flashColor = Color.red;

    [Header("Flash Settings")]
    [SerializeField] private float flashDuration = 0.25f;
    [SerializeField] private int flashCount = 2;

    private PlayerNetworkState player;
    private bool isFlashing = false;
    public static RoundHUDUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        player = PlayerNetworkState.LocalPlayer;
        if (player == null)
        {
            Debug.LogWarning("❌ RoundHUDUI: Local player not found. Retrying...");
            Invoke(nameof(Start), 0.5f);
            return;
        }

        UpdateHUD();
        InvokeRepeating(nameof(UpdateHUD), 1f, 0.5f); // Live refresh
    }

    private void UpdateHUD()
    {
        if (player == null) return;

        int round = player.CurrentRound.Value;
        int maxUnits = player.MaxUnitsAllowed;
        int placedUnits = player.PlacedUnitCount.Value;

        roundText.text = $"🔁 Round {round}";
        unitCapText.text = $"🧙 Units: {placedUnits} / {maxUnits}";
    }

    public void FlashLimitReached()
    {
        if (!isFlashing)
            StartCoroutine(FlashUnitCapRed());
    }

    private System.Collections.IEnumerator FlashUnitCapRed()
    {
        isFlashing = true;

        for (int i = 0; i < flashCount; i++)
        {
            unitCapText.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            unitCapText.color = normalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        isFlashing = false;
    }
}
