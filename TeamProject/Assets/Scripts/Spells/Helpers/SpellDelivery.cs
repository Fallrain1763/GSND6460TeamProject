using System;
using UnityEngine;

public static class SpellDelivery
{
    // -------------------------------------------------
    // INSTANT
    // Resolves immediately using the current cast context
    // -------------------------------------------------
    public static void Instant(SpellContext context, Action<SpellContext> onResolve)
    {
        if (onResolve == null) return;

        SpellContext resolvedContext = new SpellContext
        {
            origin = context.origin,
            aimDirection = context.aimDirection,
            aimPoint = context.aimPoint
        };

        onResolve.Invoke(resolvedContext);
    }

    // -------------------------------------------------
    // LOB
    // Spawns a projectile that travels in an arc and resolves on impact
    //
    // For burst:
    // - useLiveAimDirectionOnResolve = false
    // - currentAimDirectionProvider can be null
    //
    // For line/cone:
    // - useLiveAimDirectionOnResolve = true
    // - pass in a currentAimDirectionProvider
    // -------------------------------------------------
    public static void Lob(
        SpellContext context,
        GameObject projectilePrefab,
        float launchSpeed,
        float upwardArc,
        Action<SpellContext> onResolve,
        Func<Vector3> currentAimDirectionProvider = null,
        bool useLiveAimDirectionOnResolve = false)
    {
        if (projectilePrefab == null || onResolve == null) return;

        GameObject projectileObj = UnityEngine.Object.Instantiate(
            projectilePrefab,
            context.origin,
            Quaternion.identity
        );

        DeliveryLobProjectile projectile = projectileObj.GetComponent<DeliveryLobProjectile>();
        if (projectile == null)
        {
            Debug.LogWarning("SpellDelivery.Lob: projectilePrefab is missing a DeliveryLobProjectile component.");
            UnityEngine.Object.Destroy(projectileObj);
            return;
        }

        projectile.Initialize(
            context.aimDirection,
            launchSpeed,
            upwardArc,
            impactPoint =>
            {
                Vector3 resolvedDirection = context.aimDirection;

                if (useLiveAimDirectionOnResolve && currentAimDirectionProvider != null)
                {
                    Vector3 liveAimDirection = currentAimDirectionProvider.Invoke();

                    if (liveAimDirection.sqrMagnitude > 0.0001f)
                        resolvedDirection = liveAimDirection.normalized;
                }

                SpellContext resolvedContext = new SpellContext
                {
                    // For lob delivery, the impact point becomes the resolved origin
                    origin = impactPoint,

                    // For burst, this is the center point.
                    // For line/cone, this is mostly just "where the projectile landed."
                    aimPoint = impactPoint,

                    // For burst this may not matter much.
                    // For line/cone this is very important.
                    aimDirection = resolvedDirection
                };

                onResolve.Invoke(resolvedContext);
            }
        );
    }
}