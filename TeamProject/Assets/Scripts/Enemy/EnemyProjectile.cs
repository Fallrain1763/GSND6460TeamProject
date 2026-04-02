using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float damageToPlayer = 5f;
    public float speed = 10f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        GetComponent<Rigidbody>().AddForce(transform.forward * speed);
        Invoke("DestroySelf", 5f);
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision col)
    {
        PlayerHealth ph = col.gameObject.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damageToPlayer);
            DestroySelf();
        }
    }
}
