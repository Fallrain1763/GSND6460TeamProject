using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    // Assigned by Shooter at spawn — no need to set in Inspector
    [HideInInspector] public float damage = 25f;
    [HideInInspector] public float speed  = 15f;
    [HideInInspector] public float range  = 20f;

    Rigidbody rb;
    Vector3 spawnPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY  // Lock to fixed height
                         | RigidbodyConstraints.FreezeRotationX
                         | RigidbodyConstraints.FreezeRotationY
                         | RigidbodyConstraints.FreezeRotationZ;

        // Ignore collision with the Player so bullet doesn't destroy itself on spawn
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            Physics.IgnoreCollision(GetComponent<Collider>(), player.GetComponent<Collider>());
    }

    void Start()
    {
        spawnPos = transform.position;
        rb.linearVelocity = transform.forward * speed;
    }

    void Update()
    {
        // Destroy bullet once it exceeds its max range
        if (Vector3.Distance(transform.position, spawnPos) >= range)
            Destroy(gameObject);
    }

    void OnCollisionEnter(Collision col)
    {
        Enemy enemy = col.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject); // Bullet disappears on enemy hit
            return;
        }

        // Destroy on hitting anything else (walls, ground, etc.)
        // Remove this line if you don't want bullets destroyed by terrain
        Destroy(gameObject);
    }
}