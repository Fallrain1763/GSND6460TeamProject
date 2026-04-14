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

    [Header("Direct Damage")]
    public float damage = 40f;

    [Header("Burn DoT")]
    public float burnDuration = 3f;
    public float burnTickInterval = 1f;
    public float burnDamagePerTick = 5f;

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

    public override string GetTooltipDetails()
    {
        return
            "<i>Lobs an exploding fireball.</i>\n\n" +
            $"<b>Damage:</b> <color=#FF6B6B>{damage}</color>\n" +
            $"<b>DoT:</b> <color=#FF6B6B>{burnDamagePerTick/burnTickInterval} x {burnDamagePerTick}s</color>\n" +
            $"<b>Radius:</b> <color=#6BCBFF>{explosionRadius}</color>";
    }

    void OnResolved(SpellContext resolvedContext)
    {
        Collider[] hits = SpellShape.Burst(
            resolvedContext.origin,
            explosionRadius,
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

            if (burnDuration > 0f && burnDamagePerTick > 0f)
            {
                enemy.ApplyDamageOverTime(
                    burnDuration,
                    burnTickInterval,
                    burnDamagePerTick
                );
            }
        }
    }
}