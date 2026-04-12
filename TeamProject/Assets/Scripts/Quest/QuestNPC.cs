using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class QuestNPC : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRange = 3f;

    [Header("Indicator")]
    public GameObject indicator;

    [Header("Health")]
    public float maxHealth = 100f;

    float currentHealth;
    NavMeshAgent agent;
    Transform player;
    bool playerInRange;
    bool questActive;
    bool isFollowing;

    // Assigned by NPCSpawner at spawn
    [HideInInspector] public QuestData questData;
    [HideInInspector] public float questTimeout = 30f;
    [HideInInspector] public string npcName;
    [HideInInspector] public int spawnIndex;

    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        agent  = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
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

        // Follow player for Escort and Defend quests
        if (isFollowing && agent != null)
            agent.SetDestination(player.position);
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
        QuestUI.Instance?.ShowPopupThenRefresh(questData.GetDescription(npcName));

        // Only Escort and Defend NPCs follow the player
        if (questData.questType == QuestType.Escort || questData.questType == QuestType.Defend)
            StartFollowing();
    }

    public void StartFollowing()
    {
        isFollowing = true;
    }

    public void StopFollowing()
    {
        isFollowing = false;
        agent?.ResetPath();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        QuestManager.Instance?.ReportNPCDeath(questData);
        NPCSpawner.Instance?.OnNPCRemoved(this);
        Destroy(gameObject);
    }

    // Called by QuestManager on quest success
    public void OnQuestCompleted()
    {
        StopFollowing();
        NPCSpawner.Instance?.OnNPCRemoved(this);
        Destroy(gameObject);
    }

    // Called by QuestManager on timeout
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