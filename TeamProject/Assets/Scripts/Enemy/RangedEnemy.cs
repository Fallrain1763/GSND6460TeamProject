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
    
    [Header("Audio")]
    public AudioClip throwingSound;

    bool isAttacking = false;
    [SerializeField] Transform backUpPosition;
    bool isRunning = false;

    override protected void Attack()
    {
        if (ThirdPersonCamera.InputLocked) return;

        if (!isAttacking)
        {
            GetComponent<AudioSource>().PlayOneShot(throwingSound);
            animator.SetBool("walk", false);
            transform.LookAt(target);
            isAttacking = true;
            animator.SetTrigger("hit");
            StartCoroutine(AttackCooldown());
        }
    }

    override protected void ChaseTarget()
    {
        if (isRunning) return;
        float distance = Vector3.Distance(target.position, transform.position);

        if (distance < attackRange / 2)
        {
            StartCoroutine(RunAway());
        }
        else if (distance < attackRange)
        {
            agent.isStopped = true;
            currentState = EnemyState.Attack;
        }
        else
        {
            base.ChaseTarget();
        }
    }

    IEnumerator RunAway()
    {
        isRunning = true;
        ChangeTarget(backUpPosition);
        MoveToTarget();
        yield return new WaitForSecondsRealtime(5);
        isRunning = false;
    }

    IEnumerator AttackCooldown()
    {
        yield return new WaitForSecondsRealtime(animationDelay);
        Instantiate(projectile, launchPoint.position, launchPoint.rotation);
        currentState = EnemyState.Chase;
        yield return new WaitForSecondsRealtime(attackInterval);
        isAttacking = false;
    }
}