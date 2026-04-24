using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class QuestNPC : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRange = 3f;

    [Header("Indicator")]
    public GameObject indicator;

    [Header("Health")]
    public float maxHealth = 100f;

    [Header("UI")]
    public GameObject canvas;
    public TextMeshProUGUI nameText;
    public Slider healthbar;

    public Animator animator;

    float currentHealth;
    NavMeshAgent agent;
    Transform player;
    bool playerInRange;
    public void StartFollowing()
    {
        animator.SetBool("walk", true);
        isFollowing = true;
        Debug.Log($"NPC {npcName} StartFollowing, isOnNavMesh: {agent?.isOnNavMesh}, position: {transform.position}");

        if (agent != null && agent.isOnNavMesh && player != null)
            agent.SetDestination(player.position);
    }
    bool questActive;
    bool isFollowing;

    // Assigned by NPCSpawner at spawn
    [HideInInspector] public QuestData questData;
    [HideInInspector] public float questTimeout = 30f;
    [HideInInspector] public string npcName;
    [HideInInspector] public int spawnIndex;

    void Start()
    {
        nameText.text = npcName;
        player = GameObject.FindWithTag("Player")?.transform;
        agent  = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        SetMarkVisible(false);
        StartCoroutine(WarpToNavMesh());
    }

    IEnumerator WarpToNavMesh()
    {
        yield return null;

        if (agent == null) yield break;

        float[] radii = { 2f, 5f, 10f, 20f };
        foreach (float r in radii)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, r, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                agent.ResetPath();
                Debug.Log($"NPC {npcName} warped to NavMesh at {hit.position} (radius {r}), isOnNavMesh: {agent.isOnNavMesh}");
                yield break;
            }
        }

        Debug.LogWarning($"NPC {npcName} could not find NavMesh near {transform.position}");
    }

    public void WarpToPlayer()
    {
        Vector3 newPosition = player.position + new Vector3(0.5f, 0.5f, 0.5f);
        agent.Warp(newPosition);
        agent.SetDestination(player.position);
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);
        playerInRange = dist <= interactRange;
        SetMarkVisible(playerInRange && !questActive);

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
            OnInteract();

        // Follow player for Escort and Defend quests
        if (isFollowing && agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(player.position);
        }

        // Display UI based on proximity to player
        // I kinda hate that it's every update tick but it works I guess -RH
        if (playerInRange)
        {
            canvas.SetActive(true);
        }
        else
        {
            canvas.SetActive(false);
        }
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

    public void StopFollowing()
    {
        animator.SetBool("walk", false);
        isFollowing = false;
        if (agent != null && agent.isOnNavMesh)
            agent.ResetPath();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        healthbar.value = currentHealth / maxHealth;
        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        canvas.SetActive(false);
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