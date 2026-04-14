using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Earth Spike")]
public class EarthSpike : SpellBase
{
    [Header("Line")]
    public float length = 8f;
    public float radius = 1f;
    public LayerMask targetLayers = ~0;

    [Header("Effect")]
    public float damage = 25f;
    public float knockupForce = 6f;
    public float knockupDuration = 0.5f;

    public override void Cast(SpellContext context, SpellCaster caster)
    {
        SpellDelivery.Instant(context, OnResolved);
    }

    public override string GetTooltipDetails()
    {
        return
            "<i>Fires a disruptive column of rock.</i>\n\n" +
            $"<b>Damage:</b> <color=#FF6B6B>{damage}</color>\n" +
            $"<b>Knockup Strength:</b> <color=#7CFC84>{knockupForce}</color>\n" +
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

            if (knockupForce > 0f && knockupDuration > 0f)
                enemy.ApplyKnockup(knockupForce, knockupDuration);
        }
    }
}