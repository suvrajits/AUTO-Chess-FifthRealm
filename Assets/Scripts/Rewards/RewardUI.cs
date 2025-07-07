using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class RewardUI : MonoBehaviour
{
    public static RewardUI Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject rewardPanel;
    public TextMeshProUGUI rewardText;
    public Button claimButton;

    private int pendingGold = 0;

    [Header("Round Result UI")]
    public GameObject roundResultPanel;
    public TextMeshProUGUI roundResultText;

    private Coroutine hideRoundRoutine;
    private Queue<(string label, int amount)> rewardQueue = new();
    private bool isShowingReward = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        rewardPanel.SetActive(false);
        roundResultPanel.SetActive(false);
        claimButton.onClick.AddListener(OnClaim);

        StartCoroutine(RewardQueueChecker());
    }

    public void ShowReward(string label, int amount)
    {
        Debug.Log($"📩 Queued reward: {label} +{amount}g");

        rewardQueue.Enqueue((label, amount));
        TryShowNextReward();
    }

    public void QueueDelayedReward(string label, int amount, float delay)
    {
        StartCoroutine(DelayedRewardRoutine(label, amount, delay));
    }

    private IEnumerator DelayedRewardRoutine(string label, int amount, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowReward(label, amount);
    }

    private void TryShowNextReward()
    {
        if (isShowingReward)
        {
            Debug.Log("⏳ Skipping reward show — already displaying one");
            return;
        }

        if (roundResultPanel.activeSelf)
        {
            Debug.Log("⏳ Waiting for round result UI to hide");
            return;
        }

        if (rewardQueue.Count == 0) return;

        var (label, amount) = rewardQueue.Dequeue();
        pendingGold = amount;

        rewardText.text = $"{label} +{amount}g";
        rewardPanel.SetActive(true);
        isShowingReward = true;

        Debug.Log($"✅ Displaying reward: {label} +{amount}g");
    }

    private void OnClaim()
    {
        Debug.Log($"🎉 Claimed reward popup: {pendingGold}g (already granted by server)");

        // Do not add gold here — it's already added by RewardManager server-side

        pendingGold = 0;
        rewardPanel.SetActive(false);
        isShowingReward = false;

        TryShowNextReward();
    }

    public void ShowRoundResult(bool won)
    {
        if (hideRoundRoutine != null)
            StopCoroutine(hideRoundRoutine);

        roundResultText.text = won ? "🎯 You Won the Round!" : "💀 You Lost the Round.";
        roundResultText.color = won ? Color.green : Color.red;

        roundResultPanel.SetActive(true);
        hideRoundRoutine = StartCoroutine(HideRoundResultAfterDelay(2.5f));
    }

    private IEnumerator HideRoundResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        roundResultPanel.SetActive(false);

        TryShowNextReward(); // If reward is already queued, show now
    }

    private IEnumerator RewardQueueChecker()
    {
        while (true)
        {
            if (!isShowingReward && !roundResultPanel.activeSelf && rewardQueue.Count > 0)
            {
                Debug.Log("🔁 Checking reward queue: showing next reward");
                TryShowNextReward();
            }

            yield return new WaitForSeconds(0.25f);
        }
    }
}
