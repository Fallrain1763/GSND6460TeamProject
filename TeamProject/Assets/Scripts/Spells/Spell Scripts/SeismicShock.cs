using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Seismic Shock")]
public class SeismicShock : SpellBase
{
    [Header("Emanation")]
    public float radius = 4f;
    public LayerMask targetLayers = ~0;

    [Header("Effect")]
    public float damage = 20f;
    public float knockupForce = 5f;
    public float knockbackForce = 6f;
    public float forcedMovementDuration = 0.4f;

    public override void Cast(SpellContext context, SpellCaster caster)
    {
        SpellDelivery.Instant(context, OnResolved);
    }

    void OnResolved(SpellContext resolvedContext)
    {
        Collider[] hits = SpellShape.Emanation(
            resolvedContext.origin,
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
                enemy.ApplyKnockback(resolvedContext.origin, knockbackForce, forcedMovementDuration);
        }
    }
}