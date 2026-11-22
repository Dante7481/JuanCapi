using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject GameOverPanel;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI TCoin;
    public Button restartButton;
    public Button MenuButton;
    public string menuSceneName = "Menu";

    private bool gameOverActive;
    private PlayerController jugador;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Time.timeScale = 1f;
    }

    private void Start()
    {
        jugador = FindFirstObjectByType<PlayerController>();

        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(Revive);
        }

        if (MenuButton != null)
        {
            MenuButton.onClick.AddListener(GoToMenu);
        }
    }

    private void Update()
    {
        if (!gameOverActive && jugador != null && jugador.isDead)
        {
            ShowGameOver();
        }

        if (gameOverActive && Input.GetKeyDown(KeyCode.R))
        {
            Revive();
        }
    }

    private void ShowGameOver()
    {
        gameOverActive = true;

        if (gameOverText != null)
        {
            gameOverText.text = "Has muerto";
        }

        if (TCoin != null)
        {
            int totalMonedas = ContCoins.Instance != null ? ContCoins.Instance.monedas : 0;
            TCoin.text = $" {totalMonedas}";
        }

        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    private void Revive()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToMenu()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(menuSceneName))
        {
            Debug.LogError("GameManager: asigna el nombre de la escena del menú.");
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }
}
