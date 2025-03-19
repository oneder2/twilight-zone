using System.Threading;
using UnityEngine;

public class SpawnManager : Singleton<SpawnManager>
{
    public GameObject enemyPrefab; // Prepab of enemies
    public Transform spawnArea;      // Transform of respawning area
    public float spqwnInterval = 5f; // generation interval /seconds

    // local access enemy spawn collision area
    private BoxCollider2D spawnCollider;
    private float timer = 0f;

    void Start()
    {
        // arrange BoxCollider2D component of enemy spawning area to local access variable
        spawnCollider = spawnArea.GetComponent<BoxCollider2D>();
        if (spawnCollider == null)
        {
            Debug.LogError("SpawnArea have to include BoxCollider2D component！");
            return;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer > spqwnInterval)
        {
            timer = 0f;
            TrySpawnEnemy();
        }
    }

    void TrySpawnEnemy()
    {
        // check if current scene has already exist the enemy
        string enemyTag = enemyPrefab.tag;
        if (GameObject.FindGameObjectWithTag(enemyTag) == null)
        {
            // if not exist, generate new monster
            Vector2 randomPosotion = GetRandomPositionInArea();
            Instantiate(enemyPrefab, randomPosotion, Quaternion.identity);
        }
    }

    Vector2 GetRandomPositionInArea()
    {
        // get boundary of enemy respawning
        Bounds bounds = spawnCollider.bounds;

        // generate x,y value amoung the area
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);

        return new Vector2(x, y);
    }
}