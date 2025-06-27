using UnityEngine;

[System.Serializable]
public class HeroCardInstance
{
    public HeroData baseHero;
    public int starLevel = 1;

    public string DisplayName => $"{baseHero.heroName} ★{starLevel}";
}
