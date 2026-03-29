using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Fireball")]
public class Fireball : SpellBase
{
    [Header("Delivery")]
    public GameObject projectilePrefab;
    public float launchSpeed = 14f;
    public float upwardArc = 6f;

    [Header("Burst")]
    public float explosionRadius = 4f;
    public LayerMask targetLayers = ~0;

    [Header("Effect")]
    public float damage = 40f;

    public override void Cast(SpellContext context, SpellCaster caster)
    {
        SpellDelivery.Lob(
            context,
            projectilePrefab,
            launchSpeed,
            upwardArc,
            OnResolved
        );
    }

    void OnResolved(SpellContext resolvedContext)
    {
        Collider[] hits = SpellShape.Burst(
            resolvedContext.aimPoint,
            explosionRadius,
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