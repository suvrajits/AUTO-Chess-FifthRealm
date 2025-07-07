using UnityEngine;

[CreateAssetMenu(menuName = "Rewards/Reward Definition")]
public class RewardDefinition : ScriptableObject
{
    [Header("🏆 Final Placement Rewards")]
    public int placementReward1st = 20;
    public int placementReward2nd = 15;
    public int placementReward3rd = 10;

    [Header("🔁 Per-Round Rewards")]
    public int roundWinReward = 5;

    [Header("📈 Performance Bonuses")]
    public int winStreakBonus = 2;
    public int unitSurvivalReward = 1;
    public int eliminationReward = 5;
    public int mvpBonus = 10; // Top unit damage, kills etc.

    [Header("🎯 Final Match Bonus")]
    public int matchVictoryReward = 50;
}
