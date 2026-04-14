using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Glacial Rupture")]
public class GlacialRupture : SpellBase
{
    [Header("Delivery")]
    public GameObject projectilePrefab;
    public float launchSpeed = 14f;
    public float upwardArc = 6f;

    [Header("Line")]
    public float length = 8f;
    public float radius = 1f;
    public LayerMask targetLayers = ~0;

    [Header("Effect")]
    public float damage = 25f;
    public float knockbackForce = 6f;
    public float knockbackDuration = 0.4f;

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
            "<i>Lob a snowball to cause a directional fissure, knocking enemies in the direction the fissure opens</i>\n\n" +
            $"<b>Damage:</b> <color=#FF6B6B>{damage}</color>\n" +
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

            if (knockbackForce > 0f && knockbackDuration > 0f)
            {
                enemy.ApplyKnockback(
                    resolvedContext.origin,
                    knockbackForce,
                    knockbackDuration
                );
            }
        }
    }
}