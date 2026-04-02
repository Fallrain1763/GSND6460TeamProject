using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // Runtime state for one active quest
    public class ActiveQuest
    {
        public QuestData data;
        public QuestNPC sourceNPC;

        // Kill progress
        public int killProgress;
        public bool startLocationReached;  // Must reach start location before progress counts

        // Defend progress
        public float defendTimer;
        public bool defendStarted;

        // Escort progress
        public bool escortComplete;

        public bool IsComplete()
        {
            return data.questType switch
            {
                QuestType.Kill   => killProgress >= data.killCount,
                QuestType.Defend => defendStarted && defendTimer <= 0f,
                QuestType.Escort => escortComplete,
                _                => false
            };
        }

        // Progress string shown next to task in UI
        public string GetProgressString()
        {
            return data.questType switch
            {
                QuestType.Kill   => startLocationReached
                                    ? $"{killProgress}/{data.killCount} killed"
                                    : $"Go to {data.startLocationName}",
                QuestType.Defend => startLocationReached
                                    ? $"{FormatTime(defendTimer)} remaining"
                                    : $"Go to {data.startLocationName}",
                QuestType.Escort => startLocationReached
                                    ? $"Go to {data.targetLocationName}"
                                    : $"Go to {data.startLocationName}",
                _                => ""
            };
        }

        static string FormatTime(float t)
        {
            int m = Mathf.FloorToInt(t / 60f);
            int s = Mathf.FloorToInt(t % 60f);
            return $"{m:00}:{s:00}";
        }
    }

    public List<ActiveQuest> activeQuests = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        // Tick defend timers
        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            var q = activeQuests[i];
            if (q.data.questType == QuestType.Defend && q.defendStarted && q.defendTimer > 0f)
            {
                q.defendTimer -= Time.deltaTime;
                if (q.defendTimer <= 0f)
                {
                    q.defendTimer = 0f;
                    CompleteQuest(q);
                }
            }
        }
    }

    // Called by QuestNPC when player accepts quest
    public ActiveQuest AcceptQuest(QuestData data, QuestNPC npc)
    {
        var q = new ActiveQuest
        {
            data        = data,
            sourceNPC   = npc,
            defendTimer = data.defendDuration
        };
        activeQuests.Add(q);
        return q;
    }

    // Returns true if player already has an active quest from this NPC
    public bool HasQuestFromNPC(QuestNPC npc)
    {
        return activeQuests.Exists(q => q.sourceNPC == npc);
    }

    // --- Called externally to report progress ---

    // Call this from Enemy.Die() passing enemy type name
    public void ReportEnemyKilled(string enemyType)
    {
        bool changed = false;
        foreach (var q in activeQuests)
        {
            if (q.data.questType == QuestType.Kill &&
                q.startLocationReached &&
                q.data.enemyTypeName == enemyType &&
                q.killProgress < q.data.killCount)
            {
                q.killProgress++;
                changed = true;
                if (q.IsComplete()) { CompleteQuest(q); return; }
            }
        }
        if (changed) QuestUI.Instance?.RefreshQuestList();
    }

    // Call this when player enters Start Location trigger
    public void ReportStartLocationReached(QuestData data)
    {
        var q = activeQuests.Find(x => x.data == data);
        if (q == null) return;

        if (q.data.questType == QuestType.Defend && !q.defendStarted)
        {
            q.defendStarted = true;
            q.startLocationReached = true;
            QuestUI.Instance?.RefreshQuestList();
        }
        else if (q.data.questType == QuestType.Kill && !q.startLocationReached)
        {
            q.startLocationReached = true;
            QuestUI.Instance?.RefreshQuestList();
        }
        else if (q.data.questType == QuestType.Escort && !q.startLocationReached)
        {
            q.startLocationReached = true;
            QuestUI.Instance?.RefreshQuestList();
        }
    }

    // Call this when player enters Target Location trigger (Escort)
    public void ReportTargetLocationReached(QuestData data)
    {
        var q = activeQuests.Find(x => x.data == data);
        if (q == null) return;

        if (q.data.questType == QuestType.Escort && q.startLocationReached)
        {
            q.escortComplete = true;
            CompleteQuest(q);
        }
    }

    void CompleteQuest(ActiveQuest q)
    {
        Debug.Log($"Quest complete: {q.data.questName}");
        QuestUI.Instance?.ShowPopup($"Quest complete: {q.data.questName}");
        q.sourceNPC.OnQuestCompleted();
        activeQuests.Remove(q);
        QuestUI.Instance?.RefreshQuestList();
    }
}