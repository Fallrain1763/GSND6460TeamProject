using UnityEngine;

public class QuestNPC : MonoBehaviour
{
    [Header("Quest")]
    public QuestData questData;

    [Header("Interaction")]
    public float interactRange = 3f;

    [Header("Indicator")]
    public GameObject indicator;         // Drag in the sphere GameObject

    Transform player;
    bool playerInRange;
    bool questActive;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
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
            // Quest already running — show in-progress message
            QuestUI.Instance?.ShowPopup("Mission in progress");
            return;
        }

        // Accept quest
        questActive = true;
        SetMarkVisible(false);
        QuestManager.Instance.AcceptQuest(questData, this);
        QuestUI.Instance?.ShowPopupThenRefresh(questData.GetDescription());
    }

    // Called by QuestManager when quest is completed
    public void OnQuestCompleted()
    {
        questActive = false;
        SetMarkVisible(true);
    }

    void SetMarkVisible(bool visible)
    {
        if (indicator != null)
            indicator.SetActive(visible);
    }
}