using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    [Header("Reward Integration")]
    [SerializeField] SpellInventoryUI spellInventoryUI;

    [Header("NPC Defense")]
    public float npcAggroRadius = 20f;  // Only enemies within this range will target the NPC

    public static QuestManager Instance { get; private set; }

    public class ActiveQuest
    {
        public QuestData data;
        public QuestNPC sourceNPC;
        public string npcName;

        // Timeout countdown before reaching start location
        public float timeoutTimer;
        public bool  timeoutActive = true;

        // Kill progress
        public int  killProgress;
        public bool startLocationReached;

        // Defend progress
        public float defendTimer;
        public bool  defendStarted;

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
        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            var q = activeQuests[i];

            // Tick defend timer
            if (q.data.questType == QuestType.Defend && q.defendStarted && q.defendTimer > 0f)
            {
                q.defendTimer -= Time.deltaTime;
                if (q.defendTimer <= 0f)
                {
                    q.defendTimer = 0f;
                    CompleteQuest(q);
                    continue;
                }
            }

            // Tick timeout countdown (only before start location reached)
            if (q.timeoutActive && !q.startLocationReached)
            {
                q.timeoutTimer -= Time.deltaTime;
                if (q.timeoutTimer <= 0f)
                {
                    q.timeoutTimer = 0f;
                    activeQuests.RemoveAt(i);
                    QuestUI.Instance?.RefreshQuestList();
                    q.sourceNPC?.OnQuestTimeout();
                    continue;
                }
            }
        }
    }

    public ActiveQuest AcceptQuest(QuestData data, QuestNPC npc)
    {
        var q = new ActiveQuest
        {
            data          = data,
            sourceNPC     = npc,
            npcName       = npc.npcName,
            defendTimer   = data.defendDuration,
            timeoutTimer  = npc.questTimeout,
            timeoutActive = true
        };
        activeQuests.Add(q);
        ShowBeam(data.startLocationName);
        return q;
    }

    public bool HasQuestFromNPC(QuestNPC npc) =>
        activeQuests.Exists(q => q.sourceNPC == npc);

    // Called by LocationTrigger with the location's name
    public void ReportLocationReached(string locationName)
    {
        bool changed = false;

        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            var q = activeQuests[i];

            // Activate start location
            if (!q.startLocationReached && q.data.startLocationName == locationName)
            {
                q.startLocationReached = true;
                q.timeoutActive        = false;
                changed = true;
                HideBeam(q.data.startLocationName);

                if (q.data.questType == QuestType.Defend)
                    q.defendStarted = true;

                if (q.data.questType == QuestType.Escort)
                    ShowBeam(q.data.targetLocationName);

                // Switch nearby enemies to target the NPC for Escort and Defend
                if (q.data.questType == QuestType.Escort || q.data.questType == QuestType.Defend)
                {
                    q.sourceNPC.WarpToPlayer();
                    SwitchEnemiesToNPC(q.sourceNPC);
                }
            }

            // Complete escort when target location is reached
            if (q.data.questType      == QuestType.Escort &&
                q.startLocationReached &&
                !q.escortComplete &&
                q.data.targetLocationName == locationName)
            {
                q.escortComplete = true;
                CompleteQuest(q);
                return;
            }
        }

        if (changed) QuestUI.Instance?.RefreshQuestList();
    }

    public void ReportEnemyKilled(string enemyType)
    {
        bool changed = false;
        foreach (var q in activeQuests)
        {
            if (q.data.questType      == QuestType.Kill &&
                q.startLocationReached &&
                q.data.enemyTypeName  == enemyType &&
                q.killProgress        <  q.data.killCount)
            {
                q.killProgress++;
                changed = true;
                if (q.IsComplete()) { CompleteQuest(q); return; }
            }
        }
        if (changed) QuestUI.Instance?.RefreshQuestList();
    }

    // Called by QuestNPC when its health reaches zero
    public void ReportNPCDeath(QuestData data)
    {
        var q = activeQuests.Find(x => x.data == data);
        if (q == null) return;

        HideBeam(q.data.startLocationName);
        HideBeam(q.data.targetLocationName);
        activeQuests.Remove(q);
        ClearEnemyNPCTargets();
        OnMissionFail(q);
        QuestUI.Instance?.ShowPopup("Mission Failed");
        QuestUI.Instance?.RefreshQuestList();
    }

    // Called when NPC times out before player reaches start location
    public void CancelQuest(QuestData data)
    {
        var q = activeQuests.Find(x => x.data == data);
        if (q == null) return;
        activeQuests.Remove(q);
        ClearEnemyNPCTargets();
        QuestUI.Instance?.RefreshQuestList();
    }

    void CompleteQuest(ActiveQuest q)
    {
        HideBeam(q.data.startLocationName);
        HideBeam(q.data.targetLocationName);
        OnMissionSuccess(q);
        ClearEnemyNPCTargets();
        QuestUI.Instance?.ShowPopup($"Quest complete: {q.data.questName}");
        q.sourceNPC?.OnQuestCompleted();
        activeQuests.Remove(q);
        QuestUI.Instance?.RefreshQuestList();
    }

    // --- Mission outcome callbacks ---

    void OnMissionFail(ActiveQuest q)
    {
        Debug.Log($"Mission failed: {q.data.questName}");
    }

    void OnMissionSuccess(ActiveQuest q)
    {
        SpellBase rewardSpell = q.data.reward;

        if (rewardSpell == null)
        {
            Debug.Log($"Mission success! No reward spell assigned for {q.data.questName}");
            return;
        }

        bool added = false;

        if (spellInventoryUI != null)
            added = spellInventoryUI.TryAddSpell(rewardSpell);

        if (added)
        {
            Debug.Log($"Mission success! Reward granted: {rewardSpell.spellName}");
            QuestUI.Instance?.ShowPopup($"Received spell: {rewardSpell.spellName}");
        }
        else
        {
            Debug.LogWarning($"Mission success, but reward could not be added: {rewardSpell.spellName}");
            QuestUI.Instance?.ShowPopup($"Inventory full! Could not receive {rewardSpell.spellName}");
        }
    }

    // --- Location beam management ---

    LocationTrigger FindTrigger(string locationName)
    {
        foreach (var t in FindObjectsByType<LocationTrigger>(FindObjectsSortMode.None))
            if (t.locationName == locationName) return t;
        return null;
    }

    void ShowBeam(string locationName)
    {
        FindTrigger(locationName)?.SetBeamVisible(true);
    }

    void HideBeam(string locationName)
    {
        FindTrigger(locationName)?.SetBeamVisible(false);
    }

    // Only switch enemies within npcAggroRadius of the NPC
    void SwitchEnemiesToNPC(QuestNPC npc)
    {
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            float dist = Vector3.Distance(e.transform.position, npc.transform.position);
            if (dist <= npcAggroRadius)
                e.SetNPCTarget(npc.transform);
        }
    }

    void ClearEnemyNPCTargets()
    {
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            e.ClearNPCTarget();
    }
}