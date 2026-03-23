using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;           // Drag Player here

    [Header("Camera Settings")]
    public float height = 15f;         // Camera height above player
    public float smoothSpeed = 10f;    // Follow smoothness (higher = tighter)

    // Offset from player, calculated at runtime
    Vector3 offset;

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: Please assign a Target (Player) in the Inspector");
            return;
        }

        // Isometric-style angle: positioned behind and above
        offset = new Vector3(0f, height, -height * 0.6f);
        transform.position = target.position + offset;
        transform.rotation = Quaternion.Euler(60f, 0f, 0f);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            smoothSpeed * Time.deltaTime
        );
    }
}