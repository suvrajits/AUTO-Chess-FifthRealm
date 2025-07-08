using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroData", menuName = "Dota2DOW/Hero Data", order = 1)]
public class HeroData : ScriptableObject
{
    public int heroId;
    public string heroName;
    public float maxHealth;
    public float attackDamage;
    public float attackRange;
    public float attackSpeed;
    public float moveSpeed;
    public int cost;
    public Sprite heroIcon;
    public string description;
    public GameObject heroPrefab;
    public List<TraitDefinition> traits;

    [Tooltip("Delay between animation start and hit frame in seconds")]
    public float attackDelay = 0.25f;

    [Header("AI & Formation")]
    public HeroRole heroRole = HeroRole.Flexible;  // NEW: Used for role-based formation
}

public enum HeroRole
{
    Frontline,
    Backline,
    Flexible
}

