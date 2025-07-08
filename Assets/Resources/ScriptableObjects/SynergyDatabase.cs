using System.Collections.Generic;
using UnityEngine;

public class SynergyDatabase : MonoBehaviour
{
    public static SynergyDatabase Instance { get; private set; }

    public List<AdvancedSynergyDefinition> allAdvancedSynergies = new();

    public List<AdvancedSynergyDefinition> AllAdvancedSynergies => allAdvancedSynergies;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
