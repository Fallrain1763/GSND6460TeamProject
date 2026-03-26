using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class LobProjectile : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 40f;
    public float explosionRadius = 4f;

    [Header("Arc")]
    public float upwardArc = 6f;

    [Header("Lifetime")]
    public float maxLifetime = 8f;

    Rigidbody rb;
    GameObject owner;
    bool detonated = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        Destroy(gameObject, maxLifetime);
    }

    public void Launch(Vector3 forwardDir, float speed, GameObject shooter)
    {
        owner = shooter;

        rb.useGravity = true;
        rb.linearVelocity = forwardDir * speed + Vector3.up * upwardArc;

        Collider myCol = GetComponent<Collider>();
        Collider ownerCol = owner != null ? owner.GetComponent<Collider>() : null;

        if (myCol != null && ownerCol != null)
            Physics.IgnoreCollision(myCol, ownerCol);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (detonated) return;

        if (collision.gameObject == owner)
            return;

        Detonate();
    }

    void Detonate()
    {
        if (detonated) return;
        detonated = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
            }
        }

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = transform.position;
        marker.transform.localScale = Vector3.one * explosionRadius * 2f;

        Collider markerCol = marker.GetComponent<Collider>();
        if (markerCol != null)
            Destroy(markerCol);

        Destroy(marker, 0.15f);

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}