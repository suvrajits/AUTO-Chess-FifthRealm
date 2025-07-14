using UnityEngine;

public class StartupLoader : MonoBehaviour
{
    private async void Start()
    {
        await UnityServicesManager.InitUnityServicesIfNeeded();
        await SceneTransitionManager.LoadSceneAsync("LobbyScene");
    }
}
