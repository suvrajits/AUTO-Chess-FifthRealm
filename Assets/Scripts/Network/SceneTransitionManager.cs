using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Threading.Tasks;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("UI")]
    public Canvas fadeCanvas;
    public Image fadePanel;
    public TMP_Text countdownText;

    [Header("Fade Settings")]
    public float fadeDuration = 1f;
    public Color fadeColor = Color.black;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        fadeCanvas.gameObject.SetActive(false);
        SetAlpha(0f);
    }

    /// <summary>
    /// Call this to load a new scene with fade + countdown.
    /// </summary>
    public async Task LoadSceneWithCountdown(string sceneName, int countdownSeconds = 3)
    {
        fadeCanvas.gameObject.SetActive(true);
        SetAlpha(0f);
        countdownText.gameObject.SetActive(false);

        // Fade to black
        await FadeTo(1f);

        // Countdown
        for (int i = countdownSeconds; i > 0; i--)
        {
            countdownText.text = i.ToString();
            countdownText.gameObject.SetActive(true);
            await Task.Delay(1000);
        }

        countdownText.gameObject.SetActive(false);

        // Load new scene
        var loadOp = SceneManager.LoadSceneAsync(sceneName);
        while (!loadOp.isDone)
            await Task.Yield();

        // Optional: Fade from black (disable for instant)
        await FadeTo(0f);

        fadeCanvas.gameObject.SetActive(false);
    }

    private async Task FadeTo(float targetAlpha)
    {
        float startAlpha = fadePanel.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float a = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetAlpha(a);
            await Task.Yield();
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color c = fadeColor;
        c.a = alpha;
        fadePanel.color = c;
    }
}
