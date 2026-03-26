using UnityEngine;

public class Shooter : MonoBehaviour
{
    [Header("References")]
    public Camera mainCam;
    public Transform shootPoint;         // spawn point of the projectiles

    [Header("Bullet")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;
    public float bulletDamage = 25f;
    public float bulletRange = 50f;
    public float bulletLifetime = 5f;

    [Header("Aiming")]
    public float maxAimDistance = 1000f;
    public LayerMask aimLayers = ~0;

    void Start()
    {
        if (mainCam == null)
            mainCam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            Shoot();
    }

    void Shoot()
    {
        if (bulletPrefab == null || mainCam == null) return;

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayers))
            targetPoint = hit.point;
        else
            targetPoint = ray.origin + ray.direction * maxAimDistance;

        Vector3 spawnPos = shootPoint != null
            ? shootPoint.position
            : transform.position + Vector3.up * 1.2f;

        Vector3 dir = (targetPoint - spawnPos).normalized;

        GameObject bullet = Instantiate(
            bulletPrefab,
            spawnPos,
            Quaternion.LookRotation(dir)
        );

        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.damage = bulletDamage;
            b.speed = bulletSpeed;
            b.range = bulletRange;
        }

        Destroy(bullet, bulletLifetime);
    }
}