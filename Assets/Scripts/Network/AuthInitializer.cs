using UnityEngine;
using System.Threading.Tasks;

public class AuthInitializer : MonoBehaviour
{
    private async void Start()
    {
        await InitIfNeeded();
    }

    private async Task InitIfNeeded()
    {
        try
        {
            await UnityServicesManager.InitUnityServicesIfNeeded();

            Debug.Log("[AuthInitializer] ✅ Unity Services initialized by UnityServicesManager.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AuthInitializer] ❌ Failed to initialize Unity Services: " + e.Message);
        }
    }
}
