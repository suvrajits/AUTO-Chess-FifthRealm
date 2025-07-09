using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class HeroDataValidator : EditorWindow
{
    [MenuItem("Tools/TheFifthRealm/Validate HeroData Traits")]
    public static void ValidateHeroDataAssets()
    {
        string[] guids = AssetDatabase.FindAssets("t:HeroData");
        int valid = 0, warnings = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            HeroData heroData = AssetDatabase.LoadAssetAtPath<HeroData>(path);

            if (heroData == null) continue;

            if (heroData.traits == null || heroData.traits.Count == 0)
            {
                Debug.LogWarning($"⚠️ {heroData.name} has no traits assigned.");
                warnings++;
            }
            else if (heroData.traits.Count > 2)
            {
                Debug.LogWarning($"⚠️ {heroData.name} has more than 2 traits. Limit to 1–2.");
                warnings++;
            }
            else
            {
                Debug.Log($"✅ {heroData.name} has {heroData.traits.Count} trait(s): {string.Join(", ", heroData.traits.ConvertAll(t => t.traitName))}");
                valid++;
            }
        }

        Debug.Log($"🔍 HeroData Validation Complete: {valid} valid, {warnings} warnings.");
    }
}
