using System.Collections;
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
    public float wanderRange = 20;
    bool lookForNewTarget = true;

    [Header("Targeting")]
    public float aggroRadius = 10f;
    public GameObject playerObject;

    float currentHealth;
    protected NavMeshAgent agent;
    protected EnemyState currentState = EnemyState.Idle;

    // Slow variables
    float baseMoveSpeed;
    float slowTimer = 0f;
    float currentSlowPercent = 0f;

    // DoT variables
    float dotTimer = 0f;
    float dotTickTimer = 0f;
    float dotTickInterval = 1f;
    float dotDamagePerTick = 0f;

    // Stun variables
    float stunTimer = 0f;
    bool wasAgentStoppedBeforeStun = false;

    
    // Knockup variables
    float knockupTimer = 0f;
    bool knockupActive = false;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
            baseMoveSpeed = agent.speed;

        if (target == null) target = GameObject.Find("Player").transform; // I don't like using Find since it's slow but not sure what the best approach is here -RH
        if (playerObject == null) playerObject = GameObject.Find("Player");
    }

    void Update()
    {
        UpdateSlow();
        UpdateDamageOverTime();
        UpdateStun();
        UpdateKnockup();

        if (stunTimer > 0f)
        {
            animator.SetBool("walk", false);
            return;
        }

        if (knockupActive)
        {
            animator.SetBool("walk", false);
            return;
        }

        // Not sure whether this should be called every tick
        // Later, do switch statement for behavior state machine (Move, Attack, Idle, Die?) -RH

        switch (currentState)
        {
            case EnemyState.Idle:
                RandomWalk();
                ScanForTarget();
                break;
            case EnemyState.Chase:
                animator.SetBool("walk", true);
                ChaseTarget();
                break;
            case EnemyState.Attack:
                Attack();
                break;
            case EnemyState.Die:
                break;
        }
    }

    public void TakeDamage(float amount, bool triggerReaction = true)
    {
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

        currentHealth -= amount;
        Debug.Log($"{gameObject.name}: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            if (triggerReaction)
            {
                GetComponent<AudioSource>().Play();
                animator.SetTrigger("hurt"); 
            }

        }
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        if (agent == null)
            return;

        slowPercent = Mathf.Clamp01(slowPercent);
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f || slowPercent <= 0f)
            return;

        bool shouldUpdateStrength = slowPercent > currentSlowPercent;
        bool shouldUpdateDuration = duration > slowTimer;

        if (shouldUpdateStrength)
        {
            currentSlowPercent = slowPercent;
            agent.speed = baseMoveSpeed * (1f - currentSlowPercent);
        }

        if (shouldUpdateDuration)
        {
            slowTimer = duration;
        }
    }

    void UpdateSlow()
    {
        if (agent == null)
            return;

        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;

            if (slowTimer <= 0f)
            {
                slowTimer = 0f;
                currentSlowPercent = 0f;
                agent.speed = baseMoveSpeed;
            }
        }
    }

    public void ApplyDamageOverTime(float duration, float tickInterval, float damagePerTick)
    {
        duration = Mathf.Max(0f, duration);
        tickInterval = Mathf.Max(0.01f, tickInterval);
        damagePerTick = Mathf.Max(0f, damagePerTick);

        if (duration <= 0f || damagePerTick <= 0f)
            return;

        bool shouldUpdateDamage = damagePerTick > dotDamagePerTick;
        bool shouldUpdateDuration = duration > dotTimer;

        if (shouldUpdateDamage)
        {
            dotDamagePerTick = damagePerTick;
            dotTickInterval = tickInterval;
        }

        if (shouldUpdateDuration)
        {
            dotTimer = duration;
        }

        if (dotTickTimer <= 0f)
        {
            dotTickTimer = dotTickInterval;
        }
    }

    void UpdateDamageOverTime()
    {
        if (dotTimer <= 0f)
            return;

        dotTimer -= Time.deltaTime;
        dotTickTimer -= Time.deltaTime;

        if (dotTickTimer <= 0f)
        {
            TakeDamage(dotDamagePerTick, false);
            dotTickTimer = dotTickInterval;
        }

        if (dotTimer <= 0f)
        {
            dotTimer = 0f;
            dotTickTimer = 0f;
            dotDamagePerTick = 0f;
        }
    }
    public void ApplyStun(float duration)
    {
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f)
            return;

        if (stunTimer <= 0f && agent != null)
        {
            wasAgentStoppedBeforeStun = agent.isStopped;
        }

        if (duration > stunTimer)
            stunTimer = duration;

        if (agent != null)
            agent.isStopped = true;
    }

    void UpdateStun()
    {
        if (stunTimer <= 0f)
            return;

        stunTimer -= Time.deltaTime;

        if (stunTimer <= 0f)
        {
            stunTimer = 0f;

            if (agent != null)
                agent.isStopped = wasAgentStoppedBeforeStun;
        }
    }

    void BeginForcedMovement(float duration)
    {
        duration = Mathf.Max(0f, duration);
        if (duration <= 0f)
            return;

        if (duration > knockupTimer)
            knockupTimer = duration;

        knockupActive = true;

        if (agent != null && agent.enabled)
            agent.enabled = false;
    }

    public void ApplyKnockup(float upwardForce, float duration)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

        upwardForce = Mathf.Max(0f, upwardForce);
        duration = Mathf.Max(0f, duration);

        if (upwardForce <= 0f || duration <= 0f)
            return;

        BeginForcedMovement(duration);

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * upwardForce, ForceMode.VelocityChange);
    }

    void UpdateKnockup()
    {
        if (!knockupActive)
            return;

        knockupTimer -= Time.deltaTime;

        if (knockupTimer <= 0f)
        {
            knockupTimer = 0f;
            knockupActive = false;

            if (agent != null)
                agent.enabled = true;
        }
    }

    public void ApplyKnockback(Vector3 sourcePosition, float force, float duration)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

        force = Mathf.Max(0f, force);
        duration = Mathf.Max(0f, duration);

        if (force <= 0f || duration <= 0f)
            return;

        BeginForcedMovement(duration);

        Vector3 enemyPosition = transform.position;
        Vector3 awayDirection = enemyPosition - sourcePosition;

        // Flatten it so knockback stays horizontal.
        awayDirection.y = 0f;

        // Safety fallback in case source is basically on top of the enemy.
        if (awayDirection.sqrMagnitude <= 0.0001f)
            awayDirection = transform.forward;

        awayDirection.Normalize();

        Vector3 currentVelocity = rb.linearVelocity;
        currentVelocity.x = 0f;
        currentVelocity.z = 0f;
        rb.linearVelocity = currentVelocity;

        rb.AddForce(awayDirection * force, ForceMode.VelocityChange);
    }

    virtual protected void Attack()
    {
        Debug.Log("attack!");
    }

    void Die()
    {
        animator.Play("die");
        QuestManager.Instance?.ReportEnemyKilled(enemyTypeName);
        Debug.Log("Enemy died!");
        Destroy(gameObject, 5f);
    }

    void OnCollisionEnter(Collision col)
    {
        if (currentState == EnemyState.Die) return;
        PlayerHealth ph = col.gameObject.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            animator.SetTrigger("hit");
            ph.TakeDamage(damageToPlayer);
        }
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

    // Source: https://discussions.unity.com/t/solved-random-wander-ai-using-navmesh/581895/3
    void RandomWalk()
    {
        if (currentState != EnemyState.Idle) return;
        if (!lookForNewTarget) return;
        lookForNewTarget = false;
        animator.SetBool("walk", true);
        Vector3 randDirection = Random.insideUnitSphere * wanderRange;
        randDirection += transform.position;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, wanderRange, NavMesh.AllAreas);
        agent.SetDestination(navHit.position);
        StartCoroutine(WaitBeforeNewWanderTarget());
    }

    virtual protected void ChaseTarget()
    {
        ChangeTarget(playerObject.transform);
        MoveToTarget();
        ScanForTarget();
    }

    public void ChangeTarget(Transform newTarget)
    {
        agent.isStopped = true;
        target = newTarget;
        agent.isStopped = false;
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

    IEnumerator WaitBeforeNewWanderTarget()
    {
        float randomDuration = Random.Range(1,10);
        yield return new WaitForSecondsRealtime(randomDuration);
        lookForNewTarget = true;
    }
}