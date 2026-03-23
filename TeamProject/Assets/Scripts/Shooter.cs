using UnityEngine;

public class Shooter : MonoBehaviour
{
    [Header("Bullet")]
    public GameObject bulletPrefab;    // Drag bullet Prefab here
    public float bulletSpeed = 15f;
    public float bulletDamage = 25f;
    public float bulletRange = 20f;
    public float bulletLifetime = 5f;  // Fallback destroy time if bullet never hits anything

    [Header("Shoot Height")]
    public float bulletHeight = 1f;    // Fixed Y height bullets travel at

    Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            Shoot();
    }

    void Shoot()
    {
        // Cast a ray from mouse position onto a horizontal plane at bullet height
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, bulletHeight, 0f));

        if (!plane.Raycast(ray, out float dist)) return;

        Vector3 targetPoint = ray.GetPoint(dist);

        // Spawn position: same XZ as player, fixed Y height
        Vector3 spawnPos = new Vector3(transform.position.x, bulletHeight, transform.position.z);

        // Direction is XZ only — height never changes
        Vector3 dir = (targetPoint - spawnPos).normalized;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(dir));

        // Pass stats to bullet
        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.damage = bulletDamage;
            b.speed  = bulletSpeed;
            b.range  = bulletRange;
        }

        Destroy(bullet, bulletLifetime);
    }
}