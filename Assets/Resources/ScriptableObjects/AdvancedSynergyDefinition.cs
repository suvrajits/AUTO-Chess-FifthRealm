using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TheFifthRealm/AdvancedSynergy")]
public class AdvancedSynergyDefinition : ScriptableObject
{
    public string synergyName;
    public Sprite icon;
    public string description;

    public List<TraitDefinition> requiredTraits;
    public int requiredPlayerLevel;
}
