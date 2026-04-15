using UnityEngine;

[CreateAssetMenu(menuName = "Spells/FrostNova")]
public class FrostNova : SpellBase
{
    [Header("Emanation")]
    public float radius = 4f;
    public LayerMask targetLayers = ~0;

    [Header("Effect")]
    public float damage = 20f;
    public float slowPercent = 0.4f;   // 0.4 = 40% slow
    public float slowDuration = 2f;

    public override void Cast(SpellContext context, SpellCaster caster)
    {
        SpellDelivery.Instant(context, OnResolved);
    }

    public override string GetTooltipDetails()
    {
        return
            "<i>Blasts ice in every direction around you.</i>\n\n" +
            $"<b>Damage:</b> <color=#FF6B6B>{damage}</color>\n" +
            $"<b>Slow:</b> <color=#7CFC84>{slowPercent*100}% for {slowDuration}s</color>\n" +
            $"<b>Radius:</b> <color=#6BCBFF>{radius}</color>";
    }


    void OnResolved(SpellContext resolvedContext)
    {
        Collider[] hits = SpellShape.Emanation(
            resolvedContext.origin,
            radius,
            targetLayers
        );
        Instantiate(spellPrefab, resolvedContext.origin, Quaternion.identity);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null)
                continue;

            if (damage > 0f)
                enemy.TakeDamage(damage);

            if (slowDuration > 0f && slowPercent > 0f)
                enemy.ApplySlow(slowPercent, slowDuration);
        }
    }
}