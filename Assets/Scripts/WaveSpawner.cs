using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaveSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int baseEnemyCount = 3;
    public int enemiesAddedPerWave = 2;
    public float spawnRadius = 15f;
    public float timeBetweenWaves = 5f;

    // every 5 waves enemies get bigger and stronger
    public float sizeIncreasePerUpgrade   = 0.3f;
    public float healthIncreasePerUpgrade = 0.5f;
    public float damageIncreasePerUpgrade = 0.5f;

    private int currentWave = 0;
    private List<GameObject> aliveEnemies = new List<GameObject>();

    private float waveTimer = 0f;
    private bool waitingForNextWave = true;

    private void Update()
    {
        if (waitingForNextWave)
        {
            waveTimer += Time.deltaTime;

            if (waveTimer >= timeBetweenWaves)
            {
                waveTimer = 0f;
                currentWave++;
                SpawnWave();
                waitingForNextWave = false;
            }

            return;
        }

        // remove any enemies that have been killed
        aliveEnemies.RemoveAll(enemy => enemy == null);

        // if all enemies are dead, start the countdown for the next wave
        if (aliveEnemies.Count == 0)
        {
            waitingForNextWave = true;
            waveTimer = 0f;
        }
    }

    private void SpawnWave()
    {
        int enemyCount = baseEnemyCount + (currentWave - 1) * enemiesAddedPerWave;

        // counts how many time the player has reached 5 waves, which is when the enemies get stronger and bigger
        int upgradeCount = currentWave / 5;

        float sizeMultiplier   = 1f + upgradeCount * sizeIncreasePerUpgrade;
        float healthMultiplier = 1f + upgradeCount * healthIncreasePerUpgrade;
        float damageMultiplier = 1f + upgradeCount * damageIncreasePerUpgrade;

        for (int i = 0; i < enemyCount; i++)
        {
            bool foundSpot = TryGetNavMeshPosition(out Vector3 spawnPosition);
            if (!foundSpot) continue;

            GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            newEnemy.transform.localScale *= sizeMultiplier;

            EnemyAI enemyScript = newEnemy.GetComponent<EnemyAI>();
            if (enemyScript != null)
            {
                enemyScript.health *= healthMultiplier;
                enemyScript.attackDamage *= damageMultiplier;
            }

            aliveEnemies.Add(newEnemy);
        }
    }

    // tries up to 10 times to find a walkable NavMesh spot near this GameObject
    private bool TryGetNavMeshPosition(out Vector3 result)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * spawnRadius;
            randomPoint.y = transform.position.y;

            bool onNavMesh = NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas);
            if (onNavMesh)
            {
                result = hit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }
}
