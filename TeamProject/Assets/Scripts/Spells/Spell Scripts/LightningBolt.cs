using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Lightning Bolt")]
public class LightningBolt : SpellBase
{
    [Header("Line")]
    public float length = 10f;
    public float radius = 0.75f;
    public LayerMask targetLayers = ~0;

    [Header("Effect")]
    public float damage = 35f;
    public float stunDuration = 0.5f;

    public override void Cast(SpellContext context, SpellCaster caster)
    {
        SpellDelivery.Instant(context, OnResolved);
        Instantiate(spellPrefab, context.origin, Quaternion.LookRotation(context.aimDirection));
    }

    public override string GetTooltipDetails()
    {
        return
            "<i>Fires a stunning bolt of electricity.</i>\n\n" +
            $"<b>Damage:</b> <color=#FF6B6B>{damage}</color>\n" +
            $"<b>Stun Duration:</b> <color=#7CFC84>{stunDuration}s</color>\n" +
            $"<b>Length:</b> <color=#6BCBFF>{length}</color>";
    }


    void OnResolved(SpellContext resolvedContext)
    {
        Collider[] hits = SpellShape.InvisLine(
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

            if (stunDuration > 0f)
                enemy.ApplyStun(stunDuration);
        }
    }
}