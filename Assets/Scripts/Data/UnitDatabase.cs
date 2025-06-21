using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Heroes/Unit Database")]
public class UnitDatabase : ScriptableObject
{
    public List<HeroData> allHeroes;

    public static UnitDatabase Instance;

    private void OnEnable() => Instance = this;

    public HeroData GetHeroById(int id)
    {
        return allHeroes.Find(h => h.heroId == id);
    }
}
