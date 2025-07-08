using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TheFifthRealm/Trait")]
public class TraitDefinition : ScriptableObject
{
    public string traitName;
    public Sprite traitIcon;
    public string symbol;
    public string description;
    public Color uiColor;
    public AudioClip activationSound;
    public string tooltip;

    public List<TraitTierBonus> tierBonuses;
}

[System.Serializable]
public class TraitTierBonus
{
    public int requiredCount;
    public string bonusDescription;
}
