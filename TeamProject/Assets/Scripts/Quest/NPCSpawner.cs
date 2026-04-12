using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
    public static NPCSpawner Instance { get; private set; }

    [Header("Spawning")]
    public GameObject npcPrefab;
    public int maxNPCs = 5;
    public Vector3[] spawnPositions;
    public float refillDelay = 3f;

    [Header("Quest Timeout")]
    public float questTimeout = 30f;   // Y seconds to reach start location

    [Header("NPC Names")]
    public string[] firstNames = { "First1", "First2", "First3", "First4" };
    public string[] lastNames  = { "Last1",  "Last2",  "Last3",  "Last4"  };

    [Header("Quest Pools")]
    public string[] rewards     = { "Reward1", "Reward2", "Reward3", "Reward4" };
    public string[] locations   = { "Location1", "Location2", "Location3", "Location4" };
    public string[] enemyTypes  = { "Enemy1", "Enemy2", "Enemy3", "Enemy4" };
    public int[]    killCounts  = { 5, 10, 15, 20 };
    public float[]  defendTimes = { 5f, 10f, 15f, 20f };

    readonly List<QuestNPC> activeNPCs = new();
    readonly HashSet<int> occupiedIndices = new();  // Tracks which spawn positions are in use

    // Counters for quest naming (Escort1, Kill2, etc.)
    int escortCount, killCount, defendCount;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        for (int i = 0; i < maxNPCs; i++)
            SpawnOne();
    }

    // Called by QuestNPC when it is destroyed (quest complete or timeout)
    public void OnNPCRemoved(QuestNPC npc)
    {
        int idx = activeNPCs.IndexOf(npc);
        if (idx >= 0) occupiedIndices.Remove(npc.spawnIndex);
        activeNPCs.Remove(npc);
        StartCoroutine(RefillAfterDelay());
    }

    IEnumerator RefillAfterDelay()
    {
        yield return new WaitForSeconds(refillDelay);
        while (activeNPCs.Count < maxNPCs && activeNPCs.Count < spawnPositions.Length)
            SpawnOne();
    }

    void SpawnOne()
    {
        if (spawnPositions.Length == 0) return;

        // Build list of unoccupied position indices
        var available = new List<int>();
        for (int i = 0; i < spawnPositions.Length; i++)
            if (!occupiedIndices.Contains(i)) available.Add(i);

        if (available.Count == 0) return;

        int chosenIndex = available[Random.Range(0, available.Count)];
        occupiedIndices.Add(chosenIndex);

        // Sample NavMesh to get correct Y height at this position
        Vector3 spawnPos = spawnPositions[chosenIndex];
        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            spawnPos = hit.position;

        GameObject obj = Instantiate(npcPrefab, spawnPos, Quaternion.identity);
        QuestNPC npc = obj.GetComponent<QuestNPC>();
        if (npc == null) return;

        npc.spawnIndex = chosenIndex;

        // Assign name
        npc.npcName = $"{firstNames[Random.Range(0, firstNames.Length)]} "
                    + $"{lastNames[Random.Range(0, lastNames.Length)]}";

        // Assign quest and timeout
        npc.questData    = GenerateQuest();
        npc.questTimeout = questTimeout;

        // Color NPC based on quest type (red = Escort, yellow = Kill, blue = Defend)
        var rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = npc.questData.questType switch
            {
                QuestType.Escort => Color.red,
                QuestType.Kill   => Color.yellow,
                QuestType.Defend => Color.blue,
                _                => Color.white
            };
        }

        activeNPCs.Add(npc);
    }

    QuestData GenerateQuest()
    {
        var data = new QuestData();
        data.questType = (QuestType)Random.Range(0, 3);
        data.reward    = rewards[Random.Range(0, rewards.Length)];
        data.startLocationName = locations[Random.Range(0, locations.Length)];

        switch (data.questType)
        {
            case QuestType.Escort:
                data.questName = $"Escort{++escortCount}";
                // Target location must differ from start location
                var remaining = new List<string>(locations);
                remaining.Remove(data.startLocationName);
                data.targetLocationName = remaining[Random.Range(0, remaining.Count)];
                break;

            case QuestType.Kill:
                data.questName     = $"Kill{++killCount}";
                data.killCount     = killCounts[Random.Range(0, killCounts.Length)];
                data.enemyTypeName = enemyTypes[Random.Range(0, enemyTypes.Length)];
                break;

            case QuestType.Defend:
                data.questName      = $"Defend{++defendCount}";
                data.defendDuration = defendTimes[Random.Range(0, defendTimes.Length)];
                break;
        }

        return data;
    }
}