using System.Collections.Generic;
using UnityEngine;

public class UnitDatabase : MonoBehaviour
{
    public static UnitDatabase Instance { get; private set; }

    public List<HeroData> allHeroes = new();
    private Dictionary<int, HeroData> heroLookup = new();

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
        heroLookup.Clear();
        foreach (var hero in allHeroes)
        {
            heroLookup[hero.heroId] = hero;
        }

        Debug.Log($"✅ Loaded {allHeroes.Count} heroes into UnitDatabase.");
    }

    public HeroData GetHeroById(int id)
    {
        if (heroLookup.TryGetValue(id, out var hero))
            return hero;

        Debug.LogError($"❌ Hero ID {id} not found in UnitDatabase.");
        return null;
    }
}
