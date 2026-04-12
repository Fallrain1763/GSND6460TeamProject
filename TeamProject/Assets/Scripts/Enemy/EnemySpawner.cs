using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Set Up")]
    public SpawnWeightCollection spawnWeightCollection;
    public float spawnInterval;
    public int maxSpawnedEnemies; // Track with # children
    
    SpawnableToWeight[] spawnWeights;
    int[] rateThresholds;
    int thresholdsTotal = 0;

    float currentTime = 0;

    void Start()
    {
        SetUpRateThresholds(); 
    }

    void Update()
    {
        // Not sure if this is the best way to do intervals but it's my default -RH
        currentTime += Time.deltaTime;
        if (currentTime > spawnInterval)
        {
            if (transform.childCount < maxSpawnedEnemies) SpawnEnemy();
            currentTime = 0;
        }

        // Can escalate difficulty by varying spawn interval over time -RH
    }

    void SetUpRateThresholds()
    {
        spawnWeights = spawnWeightCollection.spawnWeights;
        rateThresholds = new int[spawnWeights.Length];
        int currThreshold = 0;

        for (int i = 0; i < spawnWeights.Length; i++)
        {
            thresholdsTotal += spawnWeights[i].weight;
            currThreshold += spawnWeights[i].weight;
            rateThresholds[i] = currThreshold;
        }
    }

    void SpawnEnemy()
    {
        int rand = Random.Range(0, thresholdsTotal + 1);
        
        for (int i = 0; i < rateThresholds.Length; i++)
        {
            if (rand < rateThresholds[i])
            {
                Instantiate(spawnWeights[i].spawnable, transform);
                return;
            }
        }
    }

    public void ChangeSpawnInterval(float newInterval)
    {
        spawnInterval = newInterval;
    }

    public void ChangeMaxSpawnedEnemies(int newMax)
    {
        maxSpawnedEnemies = newMax;
    }

    public void ChangeSpawnWeights(SpawnWeightCollection newWeights)
    {
        spawnWeightCollection = newWeights;
        SetUpRateThresholds();
    }
}
