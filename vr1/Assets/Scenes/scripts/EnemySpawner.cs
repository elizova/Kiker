using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject enemyPrefab;
    public float spawnInterval = 5f;
    public int maxEnemies = 10;
    public float spawnRadius = 2f;

    [Header("Wave Settings")]
    public int enemiesPerWave = 5;
    public float waveInterval = 20f;

    [Header("Enemy Setup")]
    public bool setupEnemyComponents = true;

    private int currentEnemiesCount = 0;
    private bool isSpawning = false;

    void Start()
    {
        StartSpawning();
    }

    void StartSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            StartCoroutine(SpawnWaves());
        }
    }

    IEnumerator SpawnWaves()
    {
        while (true)
        {
            yield return new WaitForSeconds(waveInterval);

            for (int i = 0; i < enemiesPerWave; i++)
            {
                if (currentEnemiesCount < maxEnemies)
                {
                    SpawnEnemy();
                    yield return new WaitForSeconds(spawnInterval);
                }
            }
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        Vector3 spawnPosition = transform.position + Random.insideUnitSphere * spawnRadius;
        spawnPosition.y = transform.position.y;

        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        if (setupEnemyComponents)
        {
            SetupEnemyComponents(enemy);
        }

        currentEnemiesCount++;

        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
        }

        Debug.Log($"Spawned enemy. Total: {currentEnemiesCount}");
    }

    void SetupEnemyComponents(GameObject enemy)
    {
        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
        }

        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.Invoke("FindClosestTower", 0.1f);
        }

        enemy.SetActive(true);
    }

    public void OnEnemyDied()
    {
        currentEnemiesCount--;
        currentEnemiesCount = Mathf.Max(0, currentEnemiesCount);
        Debug.Log($"Enemy died. Total: {currentEnemiesCount}");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}