using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 50f;
    public float damageToPlayer = 10f;

    [Header("Movement")]
    public Transform target; // Not ideal since this requires manual assignment -- might want to have globally accessible reference instead -RH

    float currentHealth;
    NavMeshAgent agent;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();

        if (target == null) target = GameObject.Find("Player").transform; // I don't like using Find since it's slow but not sure what the best approach is here -RH
    }

    void Update()
    {
        // Not sure whether this should be called every tick
        // Later, do switch statement for behavior state machine (Move, Attack, Idle, Die?) -RH
        agent.SetDestination(target.position);
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

    public void MoveToTarget()
    {
         agent.SetDestination(target.position);
    }
}