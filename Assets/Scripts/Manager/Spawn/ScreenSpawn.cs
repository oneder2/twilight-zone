using UnityEngine;

public class ScreenSpawn: MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 2f;
    public float minX = -10f, maxX = 10f;
    public float minY = -5f, maxY = 5f;
    public LayerMask obstacleLayer; // 设置为障碍物的 Layer

    void Start()
    {
        InvokeRepeating("SpawnEnemy", spawnInterval, spawnInterval);
    }

    void SpawnEnemy()
    {
        Vector2 spawnPosition = GetValidSpawnPosition();
        if (spawnPosition != Vector2.zero) // 如果找到有效位置
        {
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        }
    }

    Vector2 GetValidSpawnPosition()
    {
        float camZ = -Camera.main.transform.position.z;
        Vector3 bottomLeft = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, camZ));
        Vector3 topRight = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, camZ));

        float camLeft = bottomLeft.x;
        float camRight = topRight.x;
        float camBottom = bottomLeft.y;
        float camTop = topRight.y;

        int maxAttempts = 10; // 最大尝试次数，防止无限循环
        for (int i = 0; i < maxAttempts; i++)
        {
            int side = Random.Range(0, 4);
            float spawnX, spawnY;

            if (side == 0) // 左侧
            {
                spawnX = Random.Range(minX, camLeft);
                spawnY = Random.Range(minY, maxY);
            }
            else if (side == 1) // 右侧
            {
                spawnX = Random.Range(camRight, maxX);
                spawnY = Random.Range(minY, maxY);
            }
            else if (side == 2) // 下侧
            {
                spawnX = Random.Range(camLeft, camRight);
                spawnY = Random.Range(minY, camBottom);
            }
            else // 上侧
            {
                spawnX = Random.Range(camLeft, camRight);
                spawnY = Random.Range(camTop, maxY);
            }

            Vector2 spawnPosition = new Vector2(spawnX, spawnY);

            // 检查生成位置是否与障碍物重叠
            Collider2D hit = Physics2D.OverlapCircle(spawnPosition, 0.5f, obstacleLayer);
            if (hit == null) // 如果没有碰撞到障碍物
            {
                return spawnPosition;
            }
        }

        Debug.LogWarning("未能找到有效生成位置");
        return Vector2.zero; // 如果找不到有效位置，返回默认值
    }
}