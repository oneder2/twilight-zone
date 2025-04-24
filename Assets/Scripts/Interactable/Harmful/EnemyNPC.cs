// File: Scripts/Harmful/EnemyNPC.cs
using UnityEngine;

/// <summary>
/// Represents an enemy NPC that can harm the player.
/// 代表可以伤害玩家的敌人 NPC。
/// </summary>
public class EnemyNPC : Deadly // Assuming Deadly handles the kill logic trigger
                               // 假设 Deadly 处理击杀逻辑触发
{
    // --- Configuration set by Spawner ---
    // --- 由 Spawner 设置的配置 ---
    [HideInInspector] // Hide in inspector as it's set programmatically
                      // 在检查器中隐藏，因为它由程序设置
    public float speed; // Movement speed, set by Initialize // 移动速度，由 Initialize 设置

    // --- Private References ---
    // --- 私有引用 ---
    private Transform playerTransform;
    private EnemySpawner spawner; // Reference to the spawner that created this enemy // 对创建此敌人的生成器的引用

    // --- Unity Methods ---
    // --- Unity 方法 ---

    void Start()
    {
        // Find the player instance dynamically. Assumes Player uses Singleton pattern.
        // 动态查找玩家实例。假设 Player 使用单例模式。
        if (Player.Instance != null)
        {
            playerTransform = Player.Instance.transform;
        }
        else
        {
            Debug.LogError($"[EnemyNPC] Player instance not found!", gameObject);
            // Optionally disable self if player is missing?
            // 如果玩家丢失，可以选择禁用自身？
            enabled = false;
        }
    }

    void Update()
    {
        // Only move if the game is actively playing
        // 仅在游戏进行中时移动
        if (GameRunManager.Instance != null && GameRunManager.Instance.CurrentStatus == GameStatus.Playing)
        {
            MoveTowardsPlayer();
        }
    }

    // Called when this GameObject is destroyed
    // 当此 GameObject 被销毁时调用
    void OnDestroy()
    {
        // Notify the spawner that this enemy is gone
        // 通知生成器此敌人已消失
        if (spawner != null)
        {
            spawner.NotifyEnemyDestroyed(this);
        }
    }

    // --- Public Methods ---
    // --- 公共方法 ---

    /// <summary>
    /// Initializes the enemy with settings from the spawner.
    /// 使用生成器的设置初始化敌人。
    /// </summary>
    /// <param name="initialSpeed">The base movement speed for this enemy. / 此敌人的基础移动速度。</param>
    /// <param name="creatorSpawner">Reference to the EnemySpawner instance. / 对 EnemySpawner 实例的引用。</param>
    public void Initialize(float initialSpeed, EnemySpawner creatorSpawner)
    {
        this.speed = initialSpeed;
        this.spawner = creatorSpawner;
        Debug.Log($"[EnemyNPC] Initialized with speed: {speed}", gameObject);
    }

    // --- Movement Logic ---
    // --- 移动逻辑 ---

    private void MoveTowardsPlayer()
    {
        if (playerTransform != null)
        {
            // Calculate direction from current position to the player
            // 计算从当前位置到玩家的方向
            Vector2 direction = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;

            // Move the enemy towards the player using the assigned speed
            // 使用指定的速度将敌人移向玩家
            transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, speed * Time.deltaTime);

            // Optional: Flip sprite based on movement direction
            // 可选：根据移动方向翻转精灵图
            FlipSprite(direction.x);
        }
    }

    private void FlipSprite(float horizontalDirection)
    {
        // Basic flip logic, adjust if your sprite setup is different
        // 基本翻转逻辑，如果你的精灵图设置不同，请调整
        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>(); // Assumes sprite is on child or self
                                                                            // 假设精灵图在子对象或自身上
        if (renderer != null)
        {
            if (horizontalDirection > 0.01f)
            {
                renderer.flipX = false; // Facing right // 面向右
            }
            else if (horizontalDirection < -0.01f)
            {
                renderer.flipX = true; // Facing left // 面向左
            }
        }
    }


    // --- Collision/Damage Logic (Handled by Deadly base class or specific triggers) ---
    // --- 碰撞/伤害逻辑（由 Deadly 基类或特定触发器处理）---

    // Override KillPlayer from Deadly if specific logic is needed here
    // 如果此处需要特定逻辑，则覆盖 Deadly 中的 KillPlayer
    // override public void KillPlayer() { ... }

    // Example: Collision detection (can be handled by Deadly or here)
    // 示例：碰撞检测（可由 Deadly 或此处处理）
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if collided with the player layer
        // 检查是否与玩家图层碰撞
        if (collision.gameObject.CompareTag("Player")) // Using Tag is often more efficient than layer name check
                                                       // 使用标签通常比检查图层名称更有效
        {
            Debug.Log("[EnemyNPC] Collided with Player.", gameObject);
            // Trigger the kill logic (assuming Deadly.KillPlayer handles the Game Over state)
            // 触发击杀逻辑（假设 Deadly.KillPlayer 处理游戏结束状态）
            KillPLayer(); // Call the method from the base class (or override it)
                          // 调用基类的方法（或覆盖它）
        }
        // Optional: Add collision logic with other objects (walls, etc.)
        // 可选：添加与其他对象（墙壁等）的碰撞逻辑
    }

     // Override KillPlayer from Deadly to handle game over state change
     // 覆盖 Deadly 中的 KillPlayer 来处理游戏结束状态更改
     override public void KillPLayer()
     {
         Debug.Log($"[EnemyNPC] KillPlayer called on {gameObject.name}. Triggering Game Over.");
         // Change game status to GameOver
         // 将游戏状态更改为 GameOver
         if (GameRunManager.Instance != null)
         {
              // Prevent multiple game over triggers if multiple enemies hit simultaneously
              // 如果多个敌人同时命中，防止多次触发游戏结束
              if(GameRunManager.Instance.CurrentStatus != GameStatus.GameOver)
              {
                   GameRunManager.Instance.ChangeGameStatus(GameStatus.GameOver);
              }
         } else {
              Debug.LogError("[EnemyNPC] GameRunManager instance not found! Cannot set game over state.");
         }

         // Optional: Play death sound/effect for the player or enemy here
         // 可选：在此处播放玩家或敌人的死亡声音/效果

         // Optional: Destroy self immediately after killing player? Or let Game Over handling clean up?
         // 可选：在杀死玩家后立即销毁自身？还是让游戏结束处理来清理？
         // Destroy(gameObject);
     }
}