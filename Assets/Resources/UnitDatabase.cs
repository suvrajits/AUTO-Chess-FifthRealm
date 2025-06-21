using System.Collections.Generic;
using UnityEngine;

public class UnitDatabase : MonoBehaviour
{
    public static UnitDatabase Instance { get; private set; }

    public List<HeroData> allHeroes = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAllHeroesFromResources();
    }

    private void LoadAllHeroesFromResources()
    {
        HeroData[] heroes = Resources.LoadAll<HeroData>("Heroes");

        if (heroes == null || heroes.Length == 0)
        {
            Debug.LogError("❌ No HeroData assets found in Resources/Heroes/");
            return;
        }

        allHeroes = new List<HeroData>(heroes);
        Debug.Log($" Loaded {allHeroes.Count} heroes into UnitDatabase.");
    }

    public HeroData GetHeroById(int id)
    {
        return allHeroes.Find(h => h.heroId == id);
    }
}
