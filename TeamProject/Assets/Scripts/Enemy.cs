using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 50f;
    public float damageToPlayer = 10f;

    float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // Called by Bullet on hit
    public void TakeDamage(float amount)
    {
        // Cancel any velocity from the bullet impact
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

        currentHealth -= amount;
        Debug.Log($"Enemy HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        Debug.Log("Enemy died!");
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision col)
    {
        // Deal damage to player on contact
        PlayerHealth ph = col.gameObject.GetComponent<PlayerHealth>();
        if (ph != null)
            ph.TakeDamage(damageToPlayer);
    }
}