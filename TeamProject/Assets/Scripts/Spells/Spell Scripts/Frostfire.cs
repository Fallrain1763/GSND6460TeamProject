using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Frostfire")]
public class Frostfire : SpellBase
{
    [Header("Delivery")]
    public GameObject projectilePrefab;
    public float launchSpeed = 10f;
    public float upwardArc = 3f;

    [Header("Burst")]
    public float explosionRadius = 3f;
    public LayerMask targetLayers = ~0;

    [Header("Direct Damage")]
    public float damage = 30f;

    [Header("Burn DoT")]
    public float burnDuration = 3f;
    public float burnTickInterval = 1f;
    public float burnDamagePerTick = 4f;

    [Header("Slow")]
    [Range(0f, 1f)]
    public float slowPercent = 0.35f;
    public float slowDuration = 2f;

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
            "<i>Lob an exploding burst that slows and damages overtime</i>\n\n" +
            $"<b>Damage:</b> <color=#FF6B6B>{damage}</color>\n" +
            $"<b>Slow:</b> <color=#7CFC84>{slowPercent*100}% for {slowDuration}s</color>\n" +
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

            if (slowDuration > 0f && slowPercent > 0f)
            {
                enemy.ApplySlow(
                    slowPercent,
                    slowDuration
                );
            }
        }
    }
}