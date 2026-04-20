using UnityEngine;

public class SpawnTimer : MonoBehaviour
{
    public int spawnMax = 5;
    float currentTime = 0;
    public int spawnIncrease = 2;
    public float difficultyIncreaseIntervalInSeconds; // in seconds

    // Update is called once per frame
    void Update()
    {
        currentTime += Time.deltaTime;
        if (currentTime > difficultyIncreaseIntervalInSeconds)
        {
            spawnMax += spawnIncrease;
            currentTime = 0;
        }
    }
}
