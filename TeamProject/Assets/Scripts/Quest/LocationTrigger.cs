using UnityEngine;

public class LocationTrigger : MonoBehaviour
{
    public QuestData questData;
    public bool isTargetLocation = false;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (isTargetLocation)
            QuestManager.Instance.ReportTargetLocationReached(questData);
        else
            QuestManager.Instance.ReportStartLocationReached(questData);
    }
}