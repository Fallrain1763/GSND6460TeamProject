using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Set Up")]
    public GameObject enemyPrefab;
    public GameObject rangedEnemyPrefab;
    public float spawnInterval;

    float currentTime = 0;

    void Update()
    {
        // Not sure if this is the best way to do intervals but it's my default -RH
        currentTime += Time.deltaTime;
        if (currentTime > spawnInterval)
        {
            int rand = Random.Range(0,2);
            if (rand == 0)
                Instantiate(enemyPrefab);
            else 
                //Instantiate(rangedEnemyPrefab);
            currentTime = 0;
        }

        // Can escalate difficulty by varying spawn interval over time -RH
    }
}
