// File: Scripts/Manager/GUI/GameOverUI.cs
using UnityEngine;
using UnityEngine.UI; // Required for Button and other UI elements // UI 元素（如 Button）所需
using UnityEngine.SceneManagement; // Required for scene reloading // 场景重新加载所需
using System.Collections; // Required for Coroutines if needed for delays // 如果需要延迟，则 Coroutine 所需

/// <summary>
/// Manages the Game Over UI panel, including showing/hiding it based on game status
/// and handling button clicks for restarting or returning to the main menu.
/// 管理游戏结束 UI 面板，包括根据游戏状态显示/隐藏它，
/// 以及处理用于重新开始或返回主菜单的按钮点击。
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Assign the main Game Over panel GameObject here.")]
    // [Tooltip("请在此处分配主 Game Over 面板 GameObject。")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Assign the Restart button here.")]
    // [Tooltip("请在此处分配重新开始按钮。")]
    [SerializeField] private Button restartButton;

    [Tooltip("Assign the Main Menu button here.")]
    // [Tooltip("请在此处分配主菜单按钮。")]
    [SerializeField] private Button mainMenuButton;

    // --- Unity Methods ---
    // --- Unity 方法 ---

    void Start()
    {
        // Ensure the panel is hidden initially
        // 确保面板初始时是隐藏的
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[GameOverUI] GameOver Panel is not assigned in the Inspector!", this);
        }

        // Add listeners to the buttons
        // 为按钮添加监听器
        if (restartButton != null)
        {
            // --- MODIFICATION START ---
            // --- 修改开始 ---
            // Restart button now calls RequestRestart
            // 重新开始按钮现在调用 RequestRestart
            restartButton.onClick.AddListener(RequestRestart);
            // --- MODIFICATION END ---
        }
        else
        {
            Debug.LogError("[GameOverUI] Restart Button is not assigned!", this);
        }

        if (mainMenuButton != null)
        {
             // --- MODIFICATION START ---
            // --- 修改开始 ---
            // Main Menu button now calls RequestMainMenu
            // 主菜单按钮现在调用 RequestMainMenu
            mainMenuButton.onClick.AddListener(RequestMainMenu);
             // --- MODIFICATION END ---
        }
        else
        {
            Debug.LogError("[GameOverUI] Main Menu Button is not assigned!", this);
        }

        // Subscribe to the game status change event
        // 订阅游戏状态变化事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<GameStatusChangedEvent>(HandleGameStatusChange);
            Debug.Log("[GameOverUI] Subscribed to GameStatusChangedEvent.");
        }
        else
        {
            Debug.LogError("[GameOverUI] EventManager instance not found! Cannot listen for game status changes.");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events when this object is destroyed
        // 当此对象销毁时取消订阅事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<GameStatusChangedEvent>(HandleGameStatusChange);
             Debug.Log("[GameOverUI] Unsubscribed from GameStatusChangedEvent.");
        }

        // Clean up button listeners (good practice)
        // 清理按钮监听器（良好实践）
        // --- MODIFICATION START ---
        // --- 修改开始 ---
        if (restartButton != null) restartButton.onClick.RemoveListener(RequestRestart);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(RequestMainMenu);
         // --- MODIFICATION END ---
    }

    // --- Event Handlers ---
    // --- 事件处理器 ---

    /// <summary>
    /// Listens for game status changes and shows/hides the panel accordingly.
    /// 监听游戏状态变化并相应地显示/隐藏面板。
    /// </summary>
    private void HandleGameStatusChange(GameStatusChangedEvent eventData)
    {
        if (gameOverPanel == null) return; // Safety check // 安全检查

        if (eventData.NewStatus == GameStatus.GameOver)
        {
            // Show the Game Over panel when the game ends
            // 当游戏结束时显示 Game Over 面板
            ShowGameOverPanel();
        }
        else if (eventData.PreviousStatus == GameStatus.GameOver && eventData.NewStatus != GameStatus.GameOver)
        {
             HideGameOverPanel();
        }
    }

    // --- UI Control ---
    // --- UI 控制 ---

    private void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("[GameOverUI] Game Over Panel Shown.");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

     private void HideGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
             Debug.Log("[GameOverUI] Game Over Panel Hidden.");
        }
    }


    // --- Button Click Handlers ---
    // --- 按钮点击处理器 ---

    /// <summary>
    /// Called by the Restart button. Sets the flag and triggers the game end event.
    /// 由重新开始按钮调用。设置标志并触发游戏结束事件。
    /// </summary>
    private void RequestRestart()
    {
        Debug.Log("[GameOverUI] Restart Button Clicked. Requesting automatic restart flow.");
        GameRunManager.InitiateRestartFlow = true; // Set the flag // 设置标志
        TriggerGameEndSequence(); // Trigger the common end sequence // 触发通用的结束序列
    }

    /// <summary>
    /// Called by the Main Menu button. Clears the flag and triggers the game end event.
    /// 由主菜单按钮调用。清除标志并触发游戏结束事件。
    /// </summary>
    private void RequestMainMenu()
    {
        Debug.Log("[GameOverUI] Main Menu Button Clicked. Requesting return to main menu.");
        GameRunManager.InitiateRestartFlow = false; // Ensure the flag is false // 确保标志为 false
        TriggerGameEndSequence(); // Trigger the common end sequence // 触发通用的结束序列
    }


    /// <summary>
    /// Common logic to trigger the game end sequence.
    /// 触发游戏结束序列的通用逻辑。
    /// </summary>
    private void TriggerGameEndSequence()
    {
        // Ensure Time Scale is normal before transitioning
        // 在转换前确保时间缩放正常
        Time.timeScale = 1f;

        // Trigger the GameEndEvent. The EndGameManager (or similar) should be listening for this.
        // 触发 GameEndEvent。EndGameManager（或类似脚本）应该正在监听此事件。
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerEvent(new GameEndEvent());
            Debug.Log("[GameOverUI] Triggered GameEndEvent.");
            // The EndGameManager will handle unloading scenes and loading the main menu.
            // EndGameManager 将处理卸载场景和加载主菜单。
        }
        else
        {
             Debug.LogError("[GameOverUI] EventManager instance not found! Cannot trigger GameEndEvent.");
        }

        // Hide the panel immediately after clicking, even if transition takes time
        // 点击后立即隐藏面板，即使转换需要时间
        HideGameOverPanel();
    }

    // --- Removed old TriggerGameEnd method ---
    // --- 移除了旧的 TriggerGameEnd 方法 ---
}
