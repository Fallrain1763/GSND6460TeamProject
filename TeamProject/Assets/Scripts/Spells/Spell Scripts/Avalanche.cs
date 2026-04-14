using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Avalanche")]
public class Avalanche : SpellBase
{
    [Header("Delivery")]
    public GameObject projectilePrefab;
    public float launchSpeed = 14f;
    public float upwardArc = 6f;

    [Header("Line")]
    public float length = 12f;
    public float radius = 2.5f;
    public LayerMask targetLayers = ~0;

    [Header("Effect")]
    public float damage = 45f;
    public float knockupForce = 7f;
    public float knockbackForce = 12f;
    public float forcedMovementDuration = 0.6f;

    public override void Cast(SpellContext context, SpellCaster caster)
    {
        SpellDelivery.Lob(
            context,
            projectilePrefab,
            launchSpeed,
            upwardArc,
            OnResolved,
            caster.GetCurrentAimPoint,
            true
        );
    }

    public override string GetTooltipDetails()
    {
        return
            "<i>Lob a snowball to cause a directional avalanche, blasting enemies in the direction of the rock and snow</i>\n\n" +
            $"<b>Damage:</b> <color=#FF6B6B>{damage}</color>\n" +
            $"<b>Knockup:</b> <color=#7CFC84>{knockupForce}</color>\n" +
            $"<b>Knockback:</b> <color=#7CFC84>{knockbackForce}</color>\n" +
            $"<b>Length:</b> <color=#6BCBFF>{length}</color>";
    }


    void OnResolved(SpellContext resolvedContext)
    {
        Collider[] hits = SpellShape.Line(
            resolvedContext.origin,
            resolvedContext.aimDirection,
            length,
            radius,
            targetLayers
        );

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null)
                continue;

            if (damage > 0f)
                enemy.TakeDamage(damage);

            if (knockupForce > 0f && forcedMovementDuration > 0f)
                enemy.ApplyKnockup(knockupForce, forcedMovementDuration);

            if (knockbackForce > 0f && forcedMovementDuration > 0f)
            {
                enemy.ApplyKnockback(
                    resolvedContext.origin,
                    knockbackForce,
                    forcedMovementDuration
                );
            }
        }
    }
}