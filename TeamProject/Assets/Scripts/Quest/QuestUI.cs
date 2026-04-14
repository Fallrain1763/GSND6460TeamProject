using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance { get; private set; }

    [Header("Quest List")]
    public Transform questListParent;
    public GameObject questRowPrefab;

    [Header("Popup")]
    public TextMeshProUGUI popupText;
    public float popupHoldTime = 3f;
    public float popupFadeTime = 2f;

    readonly List<GameObject> rows = new();
    Coroutine popupCoroutine;
    Coroutine liveUpdateCoroutine;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        if (popupText != null)
        {
            var c = popupText.color; c.a = 0f; popupText.color = c;
        }
    }

    // Show popup then refresh quest list after fade completes
    public void ShowPopupThenRefresh(string message)
    {
        if (popupCoroutine != null) StopCoroutine(popupCoroutine);
        popupCoroutine = StartCoroutine(PopupThenRefreshRoutine(message));
    }

    IEnumerator PopupThenRefreshRoutine(string message)
    {
        yield return StartCoroutine(PopupRoutine(message));
        RefreshQuestList();
    }

    // Show popup only (used for "Mission in progress", quest complete, etc.)
    public void ShowPopup(string message)
    {
        if (popupText == null) return;
        if (popupCoroutine != null) StopCoroutine(popupCoroutine);
        popupCoroutine = StartCoroutine(PopupRoutine(message));
    }

    IEnumerator PopupRoutine(string message)
    {
        popupText.text = message;
        var c = popupText.color; c.a = 1f; popupText.color = c;

        yield return new WaitForSeconds(popupHoldTime);

        float elapsed = 0f;
        while (elapsed < popupFadeTime)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / popupFadeTime);
            popupText.color = c;
            yield return null;
        }
        c.a = 0f; popupText.color = c;
    }

    // Rebuild quest list rows from scratch
    public void RefreshQuestList()
    {
        foreach (var r in rows) Destroy(r);
        rows.Clear();

        foreach (var q in QuestManager.Instance.activeQuests)
        {
            var row = Instantiate(questRowPrefab, questListParent);
            var tmp = row.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = BuildRowText(q);
            rows.Add(row);
        }

        if (liveUpdateCoroutine != null) StopCoroutine(liveUpdateCoroutine);
        if (QuestManager.Instance.activeQuests.Count > 0)
            liveUpdateCoroutine = StartCoroutine(LiveUpdate());
    }

    IEnumerator LiveUpdate()
    {
        while (QuestManager.Instance.activeQuests.Count > 0)
        {
            for (int i = 0; i < rows.Count && i < QuestManager.Instance.activeQuests.Count; i++)
            {
                var tmp = rows[i].GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                    tmp.text = BuildRowText(QuestManager.Instance.activeQuests[i]);
            }
            yield return null;
        }
    }

    string BuildRowText(QuestManager.ActiveQuest q)
    {
        // Line 1: countdown + quest description
        string countdown = (!q.startLocationReached && q.timeoutActive)
            ? $"[{FormatTime(q.timeoutTimer)}] "
            : "";

        string line1 = q.data.questType switch
        {
            QuestType.Escort => $"{countdown}Escort {q.npcName} to {q.data.targetLocationName}",
            QuestType.Kill   => $"{countdown}Kill {q.data.killCount} {q.data.enemyTypeName} for {q.npcName}",
            QuestType.Defend => $"{countdown}Defend {q.npcName} for {q.data.defendDuration}s",
            _                => q.data.GetDescription(q.npcName)
        };

        // Line 2: progress
        string line2 = q.GetProgressString();

        // Line 3: reward
        string line3 = $"Reward: {q.data.reward}";

        return $"{line1}\n{line2}\n{line3}";
    }

    static string FormatTime(float t)
    {
        int m = Mathf.FloorToInt(t / 60f);
        int s = Mathf.FloorToInt(t % 60f);
        return $"{m:00}:{s:00}";
    }
}