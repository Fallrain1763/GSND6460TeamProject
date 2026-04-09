using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Lightning Bolt")]
public class LightningBolt : SpellBase
{
    [Header("Line")]
    public float length = 10f;
    public float radius = 0.75f;
    public LayerMask targetLayers = ~0;

    [Header("Effect")]
    public float damage = 35f;
    public float stunDuration = 0.5f;

    public override void Cast(SpellContext context, SpellCaster caster)
    {
        SpellDelivery.Instant(context, OnResolved);
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

            if (stunDuration > 0f)
                enemy.ApplyStun(stunDuration);
        }
    }
}