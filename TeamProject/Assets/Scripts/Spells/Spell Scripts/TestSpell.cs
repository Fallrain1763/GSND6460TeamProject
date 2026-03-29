using UnityEngine;

[CreateAssetMenu(menuName = "Spells/TestSpell")]
public class TestSpell : SpellBase
{
    [Header("Delivery")]
    public GameObject projectilePrefab;
    public float launchSpeed = 14f;
    public float upwardArc = 6f;

    [Header("Line")]
    public float lineLength = 10f;
    public float lineRadius = 0.75f;
    public LayerMask targetLayers = ~0;

    [Header("Effect")]
    public float damage = 30f;

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
            if (!hit.CompareTag("Enemy")) continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}