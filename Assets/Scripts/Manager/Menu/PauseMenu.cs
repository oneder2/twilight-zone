using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject pausePanel;


    void Start()
    {
        resumeButton.onClick.AddListener(OnResumeClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        exitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnResumeClicked()
    {
        // Deactivate pause panel object
        pausePanel.SetActive(false);
        // Resume the game.
        GameRunManager.Instance.ResumeGame();
    }

    private void OnSettingsClicked()
    {
        // Change settings of the game
        settingsPanel.SetActive(true);
    }

    private void OnExitClicked()
    {
        // Deactivate pause panel object
        pausePanel.SetActive(false);
        // End the current game session.
        GameRunManager.Instance.EndGameSession();
    }
}
