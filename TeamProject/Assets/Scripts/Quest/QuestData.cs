using UnityEngine;

public enum QuestType { Escort, Kill, Defend }

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quests/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("General")]
    public string questName;
    public QuestType questType;

    [Header("Locations")]
    public string startLocationName;   // Display name for Start Location
    public string targetLocationName;  // Display name for Target Location (Escort only)

    [Header("Kill Settings")]
    public int killCount;              // Number of enemies to kill
    public string enemyTypeName;       // Display name of enemy type (e.g. "Goblin")

    [Header("Defend Settings")]
    public float defendDuration;       // Seconds to survive

    // Returns the task description line shown in UI
    public string GetDescription()
    {
        return questType switch
        {
            QuestType.Escort => $"Escort me to {targetLocationName}",
            QuestType.Kill   => $"Kill {killCount} {enemyTypeName}",
            QuestType.Defend => $"Defend me for {defendDuration} seconds",
            _                => ""
        };
    }
}