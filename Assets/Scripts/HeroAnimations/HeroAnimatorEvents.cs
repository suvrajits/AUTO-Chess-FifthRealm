using UnityEngine;

public class HeroAnimatorEvents : MonoBehaviour
{
    private HeroCombatController combatController;

    public void Init(HeroCombatController controller)
    {
        combatController = controller;
    }

    // 👇 This must match exactly the name in your animation event
    public void OnAttackFireEvent()
    {
        Debug.Log("[HeroAnimatorEvents] 🏹 Animation event fired");

        if (combatController == null)
        {
            Debug.LogError("[HeroAnimatorEvents] ❌ combatController is NULL");
            return;
        }

        Debug.Log($"[HeroAnimatorEvents] ✅ combatController found: {combatController.name}");

        combatController.OnAttackEventTriggered();  // Should now run
    }
}

