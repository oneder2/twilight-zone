using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject settingsPanel; // 设置界面
    [SerializeField] private GameObject startGamePanel; // 游戏开始界面

    void Start()
    {
        Debug.Log("Initialized button");
        startButton.onClick.AddListener(OnStartClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

        // 初始化时隐藏子界面
        settingsPanel.SetActive(false);
    }


    private void OnStartClicked()
    {
        // Set Start game panel as active
        Debug.Log("Start clicked!");
        startGamePanel.SetActive(true);
    }

    private void OnSettingsClicked()
    {
        // Pause the game in settings mode.
        GameRunManager.Instance.PauseGame();
        // Set the settings panel to active.
        settingsPanel.SetActive(true);
    }

    private void OnQuitClicked()
    {
        Application.Quit();
    }
}