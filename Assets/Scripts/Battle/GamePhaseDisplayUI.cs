using TMPro;
using UnityEngine;

public class GamePhaseDisplayUI : MonoBehaviour
{
    public TextMeshProUGUI phaseText;

    private void Update()
    {
        if (BattleManager.Instance == null) return;
        phaseText.text = $"Phase: {BattleManager.Instance.CurrentPhase}";
    }
}
