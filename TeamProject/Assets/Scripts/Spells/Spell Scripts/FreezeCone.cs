using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Freeze Cone")]
public class FreezeCone : SpellBase
{
    [Header("Cone Shape")]
    public float range = 6f;
    public float angleDegrees = 70f;
    public LayerMask targetLayers = ~0;

    [Header("Effect")]
    public float damage = 40f;

    public override void Cast(SpellContext context, SpellCaster caster)
    {
        SpellDelivery.Instant(context, OnResolved);
    }

    void OnResolved(SpellContext resolvedContext)
    {
        Collider[] hits = SpellShape.Cone(
            resolvedContext.origin,
            resolvedContext.aimDirection,
            range,
            angleDegrees,
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