using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Plasma Lance")]
public class PlasmaLance : SpellBase
{
    [Header("Line")]
    public float length = 10f;
    public float radius = 0.75f;
    public LayerMask targetLayers = ~0;

    [Header("Direct Damage")]
    public float damage = 35f;

    [Header("Burn DoT")]
    public float burnDuration = 3f;
    public float burnTickInterval = 1f;
    public float burnDamagePerTick = 4f;

    [Header("Stun")]
    public float stunDuration = 0.25f;

    public override void Cast(SpellContext context, SpellCaster caster)
    {
        SpellDelivery.Instant(context, OnResolved);
    }

    public override string GetTooltipDetails()
    {
        return
            "<i>Fire a bolt of superheated plasma that shocks and burns</i>\n\n" +
            $"<b>Damage:</b> <color=#FF6B6B>{damage}</color>\n" +
            $"<b>Stun Duration:</b> <color=#7CFC84>{stunDuration}s</color>\n" +
            $"<b>DoT:</b> <color=#FF6B6B>{burnDamagePerTick/burnTickInterval} x {burnDamagePerTick}s</color>\n" +
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

            if (burnDuration > 0f && burnDamagePerTick > 0f)
            {
                enemy.ApplyDamageOverTime(
                    burnDuration,
                    burnTickInterval,
                    burnDamagePerTick
                );
            }

            if (stunDuration > 0f)
                enemy.ApplyStun(stunDuration);
        }
    }
}