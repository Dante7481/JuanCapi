using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public bool isPaused = false;
    public string menuSceneName = "Menu";
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }
    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }
    public void GoToMenu()
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
