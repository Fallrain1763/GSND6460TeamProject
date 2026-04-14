public enum QuestType { Escort, Kill, Defend }

// Plain class — no longer a ScriptableObject, generated at runtime by NPCSpawner
public class QuestData
{
    public string questName;
    public QuestType questType;
    public string startLocationName;
    public string targetLocationName;  // Escort only
    public int killCount;              // Kill only
    public string enemyTypeName;       // Kill only
    public float defendDuration;       // Defend only
    public SpellBase reward;

    // npcName is passed in since QuestData doesn't store it
    public string GetDescription(string npcName = "")
    {
        return questType switch
        {
            QuestType.Escort => $"Escort {npcName} to {targetLocationName}",
            QuestType.Kill   => $"Kill {killCount} {enemyTypeName} for {npcName}",
            QuestType.Defend => $"Defend {npcName} for {defendDuration} seconds",
            _                => ""
        };
    }
}