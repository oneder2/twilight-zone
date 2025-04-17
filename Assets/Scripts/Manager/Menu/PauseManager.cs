using UnityEngine;


public class PauseManager: MonoBehaviour
{
    [SerializeField] private GameObject pausePanel; // set pause panel


    void Update()
    {
        if (Input.GetButtonDown("Exit"))
        {
            if (GameRunManager.Instance.CurrentStatus.Equals(GameStatus.Playing)) // the game state is playing
            {
                // Pause game logic
                GameRunManager.Instance.PauseGame();
                // Set pause panel as active
                pausePanel.SetActive(true);
            }

            else if(GameRunManager.Instance.CurrentStatus.Equals(GameStatus.Paused)) // the game state is playing
            {   
                // Set pause panel as active
                pausePanel.SetActive(false);
                // Pause game logic
                GameRunManager.Instance.ResumeGame();
            }
        }

    }
}