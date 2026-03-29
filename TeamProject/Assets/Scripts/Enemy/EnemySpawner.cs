using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Set Up")]
    public GameObject enemyPrefab;
    public float spawnInterval;

    float currentTime = 0;

    void Update()
    {
        // Not sure if this is the best way to do intervals but it's my default -RH
        currentTime += Time.deltaTime;
        if (currentTime > spawnInterval)
        {
            Instantiate(enemyPrefab);
            currentTime = 0;
        }

        // Can escalate difficulty by varying spawn interval over time -RH
    }
}
