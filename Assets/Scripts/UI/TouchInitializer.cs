using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.EnhancedTouch;
#endif

public class TouchInitializer : MonoBehaviour
{
    void Awake()
    {
        // Legacy Input System
        Input.multiTouchEnabled = true;

#if UNITY_EDITOR
        // Re-enable mouse events in the editor for OnMouseDown and mouse clicks to work
        Input.simulateMouseWithTouches = true;
#else
        // On real device, avoid simulating mouse from touches
        Input.simulateMouseWithTouches = false;
#endif

#if ENABLE_INPUT_SYSTEM
        // New Input System: enable enhanced touch support
        EnhancedTouchSupport.Enable();
#endif
    }
}
