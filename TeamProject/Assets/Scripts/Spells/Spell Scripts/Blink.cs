using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Blink")]
public class Blink : SpellBase
{
    [Header("Blink")]
    public float maxDistance = 16f;
    public LayerMask collisionLayers = ~0;

    [Header("Safety")]
    public float surfaceOffset = 0.5f; // how far before a wall we stop

    public override void Cast(SpellContext context, SpellCaster caster)
    {
        SpellDelivery.Instant(context, ctx => OnResolved(ctx, caster));
    }

    public override string GetTooltipDetails()
    {
        return
            "<i>Teleports you forwards in the direction you're aiming.</i>\n\n" +
            $"<b>Distance:</b> <color=#6BCBFF>{maxDistance}</color>";
    }

    void OnResolved(SpellContext resolvedContext, SpellCaster caster)
    {
        if (caster == null)
            return;

        Transform playerTransform = caster.transform;
        Rigidbody rb = caster.GetComponent<Rigidbody>();

        Vector3 origin = resolvedContext.origin;
        Vector3 direction = resolvedContext.aimDirection;

        Vector3 targetPosition = origin + direction * maxDistance;

        // Raycast to prevent teleporting through walls
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, collisionLayers))
        {
            targetPosition = hit.point - direction * surfaceOffset;
        }

        // Move player
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = targetPosition;
        }
        else
        {
            playerTransform.position = targetPosition;
        }
    }
}