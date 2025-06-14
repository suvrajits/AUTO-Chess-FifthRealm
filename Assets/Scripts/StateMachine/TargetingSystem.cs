using UnityEngine;
using System.Linq;

public class TargetingSystem : MonoBehaviour
{
    public static TargetingSystem Instance;

    private void Awake()
    {
        Instance = this;
    }

    public HeroUnit FindNearestEnemy(HeroUnit seeker)
    {
        var allUnits = Object.FindObjectsByType<HeroUnit>(FindObjectsSortMode.None);

        return allUnits
            .Where(unit =>
                unit != seeker &&
                unit.Faction != seeker.Faction &&
                unit.IsAlive)
            .OrderBy(unit =>
                Vector3.Distance(seeker.transform.position, unit.transform.position))
            .FirstOrDefault();
    }

}
