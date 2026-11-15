using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    [Header("Health")]
    public Slider healthSlider;
    public TMP_Text healthText;

    [Header("Mission UI")]
    public TMP_Text missionMessageText;
    public TMP_Text interactionPromptText;

    [Header("Console Messages")]
    public bool showConsoleLogs = false;
    public bool showWarningsAndErrors = false;
    public TMP_Text consoleMessageText;

    [Header("Game Over")]
    public GameObject gameOverPanel;

    private void Awake()
    {
        if (missionMessageText != null)
            missionMessageText.gameObject.SetActive(false);

        if (interactionPromptText != null)
            interactionPromptText.gameObject.SetActive(false);

        if (consoleMessageText != null)
            consoleMessageText.gameObject.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleUnityLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleUnityLog;
    }

    private void HandleUnityLog(
        string message,
        string stackTrace,
        LogType type)
    {
        if (!showConsoleLogs || consoleMessageText == null)
            return;

        if (type != LogType.Log && !showWarningsAndErrors)
            return;

        consoleMessageText.text = message;
        consoleMessageText.gameObject.SetActive(true);
    }

    public void UpdateHealth(float current, float maximum)
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = maximum;
            healthSlider.value = current;
        }

        if (healthText != null)
        {
            healthText.text =
                $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
        }
    }

    public void ShowMessage(string message)
    {
        Debug.Log(message);
        DisplayMessage(message);
    }

    private void DisplayMessage(string message)
    {
        if (missionMessageText == null)
            return;

        missionMessageText.text = message;
        missionMessageText.gameObject.SetActive(true);
    }

    public void SetInteractionPrompt(
        bool visible,
        string message = "Press E to rescue hiker")
    {
        if (interactionPromptText == null)
            return;

        interactionPromptText.text = message;
        interactionPromptText.gameObject.SetActive(visible);
    }

    public void ShowGameOver()
    {
        SetInteractionPrompt(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }
}
