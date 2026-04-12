using UnityEngine;

[CreateAssetMenu(fileName = "SpawnWeightCollection", menuName = "Scriptable Objects/SpawnWeightCollection")]
public class SpawnWeightCollection : ScriptableObject
{
    public SpawnableToWeight[] spawnWeights;
}
