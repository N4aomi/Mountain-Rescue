using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("References")]
    public GameHUD hud;
    public PlayerManager playerManager;
    public InputManager inputManager;
    public PauseMenu pauseMenu;

    public bool IsDead { get; private set; }

    private void Start()
    {
        Time.timeScale = 1f;
        currentHealth = Mathf.Max(1f, maxHealth);
        IsDead = false;

        if (playerManager == null)
            playerManager = GetComponent<PlayerManager>();

        if (inputManager == null)
            inputManager = GetComponent<InputManager>();

        if (hud != null)
            hud.UpdateHealth(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (hud != null)
            hud.UpdateHealth(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        IsDead = true;

        if (hud != null)
        {
            hud.ShowMessage("You were defeated by the boar!");
            hud.ShowGameOver();
        }

        if (playerManager != null)
            playerManager.enabled = false;

        if (inputManager != null)
            inputManager.enabled = false;

        if (pauseMenu != null)
            pauseMenu.enabled = false;

        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
