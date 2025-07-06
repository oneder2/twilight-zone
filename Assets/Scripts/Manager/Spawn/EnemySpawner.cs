// File: Scripts/Manager/Spawn/EnemySpawner.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the spawning of enemies based on the current game stage configuration.
/// 根据当前游戏阶段配置管理敌人的生成。
/// Assumes Singleton pattern for easy access by StageManager.
/// 假设使用单例模式以便 StageManager 轻松访问。
/// </summary>
public class EnemySpawner : Singleton<EnemySpawner> // Inherit from your Singleton base class // 继承自你的 Singleton 基类
{
    [Header("生成设置 (Spawn Settings)")]
    [Tooltip("生成区域距离屏幕边缘的最小/最大距离 (Min/Max distance from screen edge for spawning)")]
    [SerializeField] private float minSpawnOffset = 1f; // 距离屏幕边缘的最小距离 (Min distance from screen edge)
    [SerializeField] private float maxSpawnOffset = 3f; // 距离屏幕边缘的最大距离 (Max distance from screen edge)

    [Tooltip("尝试寻找有效生成位置的最大次数 (Max attempts to find a valid spawn position)")]
    [SerializeField] private int maxSpawnAttempts = 10;

    [Tooltip("用于检测生成点障碍物的层 (Layer mask for checking obstacles at spawn points)")]
    [SerializeField] private LayerMask obstacleLayer; // 设置为障碍物的 Layer (Layer for obstacles)

    // --- Internal State ---
    // --- 内部状态 ---
    private StageData currentStageData; // 当前阶段的配置数据 (Configuration data for the current stage)
    private Coroutine spawnLoopCoroutine; // 对生成循环协程的引用 (Reference to the spawn loop coroutine)
    private List<EnemyNPC> activeEnemies = new List<EnemyNPC>(); // 当前活跃的敌人列表 (List of currently active enemies)
    private Camera mainCamera; // 主摄像机的缓存引用 (Cached reference to the main camera)

    // --- Unity Methods ---
    // --- Unity 方法 ---

    protected override void Awake()
    {
        base.Awake(); // Ensure Singleton setup // 确保单例设置
        mainCamera = Camera.main; // Cache the main camera // 缓存主摄像机
        if (mainCamera == null)
        {
            Debug.LogError("[EnemySpawner] Main Camera not found!", this);
        }
    }

    // --- Public Configuration Method ---
    // --- 公共配置方法 ---

    /// <summary>
    /// Configures the spawner based on the provided stage data. Stops the previous spawn loop and starts a new one.
    /// 根据提供的阶段数据配置生成器。停止之前的生成循环并启动新的循环。
    /// </summary>
    /// <param name="stageData">The data for the current stage. / 当前阶段的数据。</param>
    public void ConfigureSpawner(StageData stageData)
    {
        if (stageData == null)
        {
            Debug.LogError("[EnemySpawner] ConfigureSpawner called with null StageData!", this);
            return;
        }

        currentStageData = stageData;
        Debug.Log($"[EnemySpawner] Configuring for Stage: {currentStageData.stageName}. Interval: {currentStageData.enemySpawnInterval}, Max Enemies: {currentStageData.maxActiveEnemies}, Prefab: {currentStageData.enemyPrefab?.name ?? "None"}", this);

        // Stop the existing spawn loop if it's running
        // 如果生成循环正在运行，则停止它
        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = null;
            Debug.Log("[EnemySpawner] Stopped previous spawn loop.", this);
        }

        // Start a new spawn loop if the interval is valid and prefab exists
        // 如果间隔有效且预制件存在，则启动新的生成循环
        if (currentStageData.enemySpawnInterval > 0 && currentStageData.enemyPrefab != null && currentStageData.maxActiveEnemies > 0)
        {
            spawnLoopCoroutine = StartCoroutine(SpawnLoopCoroutine());
            Debug.Log("[EnemySpawner] Started new spawn loop.", this);
        }
        else
        {
             Debug.LogWarning($"[EnemySpawner] Spawning disabled for stage '{currentStageData.stageName}' due to invalid interval ({currentStageData.enemySpawnInterval}), max enemies ({currentStageData.maxActiveEnemies}), or missing prefab.", this);
             // Ensure no enemies remain from previous stages if spawning is now disabled
             // 如果现在禁用了生成，确保没有来自先前阶段的敌人残留
             ClearAllSpawnedEnemies();
        }
    }

    // --- Spawning Logic ---
    // --- 生成逻辑 ---

    /// <summary>
    /// Coroutine that periodically attempts to spawn enemies based on the current configuration.
    /// 根据当前配置定期尝试生成敌人的协程。
    /// </summary>
    private IEnumerator SpawnLoopCoroutine()
    {
        // Initial delay before first spawn (optional)
        // 首次生成前的初始延迟（可选）
        // yield return new WaitForSeconds(currentStageData.enemySpawnInterval / 2f);

        while (true) // Loop indefinitely, controlled by stopping the coroutine
                     // 无限循环，通过停止协程来控制
        {
            // Wait for the specified interval
            // 等待指定的时间间隔
            yield return new WaitForSeconds(currentStageData.enemySpawnInterval);

            // Check game state - only spawn when playing
            // 检查游戏状态 - 仅在游戏进行中时生成
            if (GameRunManager.Instance != null && GameRunManager.Instance.CurrentStatus == GameStatus.Playing)
            {
                TrySpawnEnemy();
            }
            else
            {
                 // Optional: Log if skipping spawn due to game state
                 // 可选：如果由于游戏状态而跳过生成，则记录日志
                 // Debug.Log("[EnemySpawner] Skipping spawn attempt, game not in Playing state.");
            }
        }
    }

    /// <summary>
    /// Attempts to spawn a single enemy if conditions are met (max count, valid position).
    /// 如果满足条件（最大数量、有效位置），则尝试生成单个敌人。
    /// </summary>
    private void TrySpawnEnemy()
    {
        // Check if the maximum number of active enemies has been reached
        // 检查是否已达到最大活跃敌人数量
        if (activeEnemies.Count >= currentStageData.maxActiveEnemies)
        {
            // Debug.Log($"[EnemySpawner] Max enemy count ({activeEnemies.Count}/{currentStageData.maxActiveEnemies}) reached. Skipping spawn.", this);
            return;
        }

        // Check if enemy prefab is valid
        // 检查敌人预制件是否有效
        if (currentStageData.enemyPrefab == null)
        {
             Debug.LogError("[EnemySpawner] Cannot spawn enemy, enemyPrefab is null in current StageData!", this);
             // Consider stopping the coroutine if prefab is missing?
             // 如果预制件丢失，考虑停止协程？
             if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
             return;
        }


        // Find a valid spawn position outside the camera view
        // 在摄像机视图外查找有效的生成位置
        Vector2 spawnPosition = GetValidSpawnPosition();

        if (spawnPosition != Vector2.zero) // Check if a valid position was found // 检查是否找到了有效位置
        {
            // Instantiate the enemy prefab at the calculated position
            // 在计算出的位置实例化敌人预制件
            GameObject enemyObject = Instantiate(currentStageData.enemyPrefab, spawnPosition, Quaternion.identity);
            EnemyNPC newEnemy = enemyObject.GetComponent<EnemyNPC>();

            if (newEnemy != null)
            {
                // Initialize the enemy with stage-specific data and a reference to this spawner
                // 使用特定阶段的数据和对此生成器的引用来初始化敌人
                newEnemy.Initialize(currentStageData.enemyBaseSpeed, this);

                // Add the newly spawned enemy to the tracking list
                // 将新生成的敌人添加到追踪列表
                activeEnemies.Add(newEnemy);
                // Debug.Log($"[EnemySpawner] Spawned enemy '{newEnemy.name}' at {spawnPosition}. Active count: {activeEnemies.Count}", this);
            }
            else
            {
                Debug.LogError($"[EnemySpawner] Spawned prefab '{currentStageData.enemyPrefab.name}' does not contain an EnemyNPC component!", enemyObject);
                Destroy(enemyObject); // Clean up invalid spawn // 清理无效的生成
            }
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] Failed to find a valid spawn position after multiple attempts. Skipping spawn.", this);
        }
    }

    /// <summary>
    /// Calculates a random spawn position just outside the main camera's view.
    /// 计算主摄像机视图外的随机生成位置。
    /// </summary>
    /// <returns>A valid spawn position, or Vector2.zero if none found. / 一个有效的生成位置，如果找不到则返回 Vector2.zero。</returns>
    private Vector2 GetValidSpawnPosition()
    {
        if (mainCamera == null) return Vector2.zero; // Cannot calculate without camera // 没有摄像机无法计算

        float camHeight = 2f * mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;
        Vector2 camCenter = mainCamera.transform.position;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            float spawnX = 0, spawnY = 0;
            float offset = Random.Range(minSpawnOffset, maxSpawnOffset); // Random distance outside screen // 屏幕外的随机距离

            // Choose a random side (0: Left, 1: Right, 2: Bottom, 3: Top)
            // 选择一个随机边（0：左，1：右，2：下，3：上）
            int side = Random.Range(0, 4);

            switch (side)
            {
                case 0: // Left // 左
                    spawnX = camCenter.x - (camWidth / 2f) - offset;
                    spawnY = Random.Range(camCenter.y - (camHeight / 2f), camCenter.y + (camHeight / 2f));
                    break;
                case 1: // Right // 右
                    spawnX = camCenter.x + (camWidth / 2f) + offset;
                    spawnY = Random.Range(camCenter.y - (camHeight / 2f), camCenter.y + (camHeight / 2f));
                    break;
                case 2: // Bottom // 下
                    spawnX = Random.Range(camCenter.x - (camWidth / 2f), camCenter.x + (camWidth / 2f));
                    spawnY = camCenter.y - (camHeight / 2f) - offset;
                    break;
                case 3: // Top // 上
                    spawnX = Random.Range(camCenter.x - (camWidth / 2f), camCenter.x + (camWidth / 2f));
                    spawnY = camCenter.y + (camHeight / 2f) + offset;
                    break;
            }

            Vector2 potentialPosition = new Vector2(spawnX, spawnY);

            // Check if the potential position overlaps with any obstacles
            // 检查潜在位置是否与任何障碍物重叠
            // Use a small radius for the check to avoid spawning inside walls
            // 使用小半径进行检查，以避免在墙内生成
            Collider2D hit = Physics2D.OverlapCircle(potentialPosition, 0.5f, obstacleLayer);
            if (hit == null) // If no obstacle is hit, the position is valid // 如果没有碰到障碍物，则该位置有效
            {
                return potentialPosition;
            }
        }

        // Return zero vector if no valid position found after attempts
        // 如果尝试后未找到有效位置，则返回零向量
        return Vector2.zero;
    }

    // --- Enemy Lifecycle Management ---
    // --- 敌人生命周期管理 ---

    /// <summary>
    /// Called by EnemyNPC instances when they are destroyed. Removes them from the active list.
    /// 当 EnemyNPC 实例被销毁时由其调用。将它们从活动列表中移除。
    /// </summary>
    /// <param name="enemy">The enemy instance that was destroyed. / 被销毁的敌人实例。</param>
    public void NotifyEnemyDestroyed(EnemyNPC enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
            // Debug.Log($"[EnemySpawner] Enemy '{enemy.name}' destroyed and removed from list. Active count: {activeEnemies.Count}", this);
        }
    }

    /// <summary>
    /// Destroys all enemies currently tracked by this spawner. Used when stopping spawning or resetting.
    /// 销毁此生成器当前追踪的所有敌人。在停止生成或重置时使用。
    /// </summary>
    private void ClearAllSpawnedEnemies()
    {
         Debug.Log($"[EnemySpawner] Clearing {activeEnemies.Count} active enemies.");
         // Create a copy to iterate over, as NotifyEnemyDestroyed modifies the list
         // 创建一个副本进行迭代，因为 NotifyEnemyDestroyed 会修改列表
         List<EnemyNPC> enemiesToClear = new List<EnemyNPC>(activeEnemies);
         foreach(EnemyNPC enemy in enemiesToClear)
         {
              if(enemy != null) // Check if it wasn't already destroyed somehow // 检查它是否尚未以某种方式被销毁
              {
                   Destroy(enemy.gameObject);
              }
         }
         // The OnDestroy->NotifyEnemyDestroyed calls should clear the original list
         // OnDestroy->NotifyEnemyDestroyed 调用应该会清除原始列表
         // As a safeguard, clear it directly too:
         // 作为保障措施，也直接清除它：
         activeEnemies.Clear();
    }

    // Optional: Clear enemies when the spawner itself is disabled or destroyed
    // 可选：在生成器本身被禁用或销毁时清除敌人
    // void OnDisable()
    // {
    //     if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
    //     ClearAllSpawnedEnemies();
    // }
}
