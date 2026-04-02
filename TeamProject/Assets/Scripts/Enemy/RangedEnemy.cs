using System.Collections;
using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Ranged Attack")]
    public float attackRange = 5f;
    public float rangedDamage = 10f;
    public GameObject projectile;
    public Transform launchPoint;
    public float animationDelay = 1.5f;
    public float attackInterval = 5f;

    bool isAttacking = false;

    override protected void Attack()
    {
        if (!isAttacking)
        {
            transform.LookAt(target);
            isAttacking = true;
            animator.SetTrigger("hit");
            StartCoroutine(AttackCooldown());
        }
    }

    override protected void ChaseTarget()
    {
        if (Vector3.Distance(target.position, transform.position) <= attackRange)
        {
            agent.isStopped = true;
            currentState = EnemyState.Attack;
        }
        else
        {
            base.ChaseTarget();
        }
    }

    IEnumerator AttackCooldown()
    {
        yield return new WaitForSecondsRealtime(animationDelay);
        Instantiate(projectile, launchPoint.position, launchPoint.rotation);
        yield return new WaitForSecondsRealtime(attackInterval);
        isAttacking = false;
        currentState = EnemyState.Chase;
    }
}
