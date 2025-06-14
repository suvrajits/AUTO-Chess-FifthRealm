using UnityEngine;

[CreateAssetMenu(fileName = "HeroData", menuName = "Dota2DOW/Hero Data", order = 1)]

public class HeroData : ScriptableObject
{
    public string heroName;
    public float maxHealth;
    public float attackDamage;
    public float attackRange;
    public float attackSpeed;
    public float moveSpeed;
    public GameObject heroPrefab; // Prefab reference for this hero
    [Tooltip("Delay between animation start and hit frame in seconds")]
    public float attackDelay = 0.25f;
}
