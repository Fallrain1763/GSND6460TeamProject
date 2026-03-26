using UnityEngine;

public class Fireball : MonoBehaviour
{
    [Header("References")]
    public Camera mainCam;
    public Transform castPoint;
    public GameObject lobProjectilePrefab;

    [Header("Aiming")]
    public float maxAimDistance = 1000f;
    public LayerMask aimLayers = ~0;

    [Header("Launch")]
    public float launchSpeed = 14f;

    void Start()
    {
        if (mainCam == null)
            mainCam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            CastLob();
        }
    }

    void CastLob()
    {
        if (lobProjectilePrefab == null || mainCam == null) return;

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayers))
            targetPoint = hit.point;
        else
            targetPoint = ray.origin + ray.direction * 25f;

        Vector3 spawnPos = castPoint != null
            ? castPoint.position
            : transform.position + Vector3.up * 1.2f;

        Vector3 launchDir = (targetPoint - spawnPos).normalized;

        GameObject proj = Instantiate(
            lobProjectilePrefab,
            spawnPos,
            Quaternion.identity
        );

        LobProjectile lob = proj.GetComponent<LobProjectile>();
        if (lob != null)
        {
            lob.Launch(launchDir, launchSpeed, gameObject);
        }
    }
}