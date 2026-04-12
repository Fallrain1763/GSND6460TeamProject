using UnityEngine;

public class QuestNPC : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRange = 3f;

    [Header("Indicator")]
    public GameObject indicator;

    // Assigned by NPCSpawner at spawn
    [HideInInspector] public QuestData questData;
    [HideInInspector] public float questTimeout = 30f;
    [HideInInspector] public string npcName;
    [HideInInspector] public int spawnIndex;  // Which position in NPCSpawner.spawnPositions

    Transform player;
    bool playerInRange;
    bool questActive;

    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        SetMarkVisible(false);
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);
        playerInRange = dist <= interactRange;
        SetMarkVisible(playerInRange && !questActive);

        if (playerInRange && Input.GetKeyDown(KeyCode.Space))
            OnInteract();
    }

    void OnInteract()
    {
        if (questActive)
        {
            QuestUI.Instance?.ShowPopup("Mission in progress");
            return;
        }

        questActive = true;
        SetMarkVisible(false);
        QuestManager.Instance.AcceptQuest(questData, this);

        // Show NPC name alongside quest description in popup
        QuestUI.Instance?.ShowPopupThenRefresh(questData.GetDescription(npcName));
    }

    // Called by QuestManager when quest is successfully completed
    public void OnQuestCompleted()
    {
        NPCSpawner.Instance?.OnNPCRemoved(this);
        Destroy(gameObject);
    }

    // Called by QuestManager when the timeout expires before reaching start location
    public void OnQuestTimeout()
    {
        QuestManager.Instance.CancelQuest(questData);
        NPCSpawner.Instance?.OnNPCRemoved(this);
        Destroy(gameObject);
    }

    void SetMarkVisible(bool visible)
    {
        if (indicator != null)
            indicator.SetActive(visible);
    }
}