using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGameMenu : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Image backgroundImage;

    void Start()
    {
        newGameButton.onClick.AddListener(OnNewGameClicked);
        backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnNewGameClicked()
    {
        // Start a game
        GameRunManager.Instance.ChangeGameStatus(GameStatus.Playing);
    }

    private void OnBackClicked()
    {
        // Back to main game or menu
        gameObject.SetActive(false);
    }
}