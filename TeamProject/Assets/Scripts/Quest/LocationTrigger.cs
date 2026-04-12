using UnityEngine;

public class LocationTrigger : MonoBehaviour
{
    [Tooltip("Must exactly match the location name used in quest generation (e.g. 'Location1')")]
    public string locationName;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        QuestManager.Instance.ReportLocationReached(locationName);
    }
}