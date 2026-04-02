using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Idle,
    Chase,
    Attack,
    Die
}

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 50f;
    public float damageToPlayer = 10f;

    [Header("Type")]
    public string enemyTypeName;

    [Header("Movement")]
    public Transform target; // Not ideal since this requires manual assignment -- might want to have globally accessible reference instead -RH
    public Animator animator;

    [Header("Targeting")]
    public float aggroRadius = 10f;

    float currentHealth;
    protected NavMeshAgent agent;
    protected EnemyState currentState = EnemyState.Idle;

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

        switch (currentState)
        {
            case EnemyState.Idle:
                ScanForTarget();
                break;
            case EnemyState.Chase:
                ChaseTarget();
                break;
            case EnemyState.Attack:
                Attack();
                break;
            case EnemyState.Die:
                break;
        }
    }

    public void TakeDamage(float amount)
    {
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

        currentHealth -= amount;
        Debug.Log($"Enemy HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    virtual protected void Attack()
    {
        Debug.Log("attack!");
    }

    void Die()
    {
        QuestManager.Instance?.ReportEnemyKilled(enemyTypeName);
        Debug.Log("Enemy died!");
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision col)
    {
        animator.SetTrigger("hit");
        PlayerHealth ph = col.gameObject.GetComponent<PlayerHealth>();
        if (ph != null)
            ph.TakeDamage(damageToPlayer);
    }

    void ScanForTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, aggroRadius);
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.tag == "Player")
                currentState = EnemyState.Chase;
            else
                currentState = EnemyState.Idle;
        }
    }

    virtual protected void ChaseTarget()
    {
        agent.SetDestination(target.position);
        ScanForTarget();
    }

    public void ChangeTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void MoveToTarget()
    {
        agent.SetDestination(target.position);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
    }
}