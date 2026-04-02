using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Frostfire Burst")]
public class FrostfireBurst : SpellBase
{
    [Header("Burst Shape")]
    public float radius = 3f;
    public LayerMask targetLayers = ~0;

    [Header("Effect")]
    public float damage = 25f;

    public override void Cast(SpellContext context, SpellCaster caster)
    {
        SpellDelivery.Instant(context, OnResolved);
    }

    void OnResolved(SpellContext resolvedContext)
    {
        Collider[] hits = SpellShape.Burst(
            resolvedContext.aimPoint,
            radius,
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