using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Static Freeze")]
public class StaticFreeze : SpellBase
{
    [Header("Burst")]
    public float radius = 4f;
    public float maxRange = 12f;
    public LayerMask targetLayers = ~0;

    [Header("Effect")]
    public float damage = 25f;

    [Range(0f, 1f)]
    public float slowPercent = 0.4f;
    public float slowDuration = 2.5f;

    public override void Cast(SpellContext context, SpellCaster caster)
    {
        SpellDelivery.Instant(context, OnResolved);
    }

    public override string GetTooltipDetails()
    {
        return
            "<i>Channel electricity through snow to create a burst where you're looking</i>\n\n" +
            $"<b>Damage:</b> <color=#FF6B6B>{damage}</color>\n" +
            $"<b>Slow:</b> <color=#7CFC84>{slowPercent*100}% for {slowDuration}s</color>\n" +
            $"<b>Range:</b> <color=#6BCBFF>{maxRange}</color>\n" +
            $"<b>Radius:</b> <color=#6BCBFF>{radius}</color>";
    }

    void OnResolved(SpellContext resolvedContext)
    {
        Vector3 burstCenter = SpellTargeting.ClampPointToRange(
            resolvedContext.origin,
            resolvedContext.aimPoint,
            maxRange
        );

        Collider[] hits = SpellShape.Burst(
            burstCenter,
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

            if (slowPercent > 0f && slowDuration > 0f)
                enemy.ApplySlow(slowPercent, slowDuration);
        }
    }
}