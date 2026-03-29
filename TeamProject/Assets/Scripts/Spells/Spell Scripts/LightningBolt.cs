using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Lightning Bolt")]
public class LightningBolt : SpellBase
{
    [Header("Line")]
    public float lineLength = 12f;
    public float lineRadius = 0.75f;
    public LayerMask targetLayers = ~0;

    [Header("Effect")]
    public float damage = 25f;

    public override void Cast(SpellContext context, SpellCaster caster)
    {
        SpellDelivery.Instant(context, OnResolved);
    }

    void OnResolved(SpellContext resolvedContext)
    {
        Collider[] hits = SpellShape.Line(
            resolvedContext.origin,
            resolvedContext.aimDirection,
            lineLength,
            lineRadius,
            targetLayers
        );

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
            }
        }
    }
}