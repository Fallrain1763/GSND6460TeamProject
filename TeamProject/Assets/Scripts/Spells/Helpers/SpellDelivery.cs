using System;
using UnityEngine;

public static class SpellDelivery
{
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

    public static void Lob(
        SpellContext context,
        GameObject projectilePrefab,
        float launchSpeed,
        float upwardArc,
        Action<SpellContext> onResolve,
        Func<Vector3> currentAimPointProvider = null,
        bool useLiveAimPointOnResolve = false)
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
                Vector3 resolvedAimPoint = impactPoint;
                Vector3 resolvedDirection = context.aimDirection;

                if (useLiveAimPointOnResolve && currentAimPointProvider != null)
                {
                    resolvedAimPoint = currentAimPointProvider.Invoke();

                    Vector3 toAimPoint = resolvedAimPoint - impactPoint;
                    if (toAimPoint.sqrMagnitude > 0.0001f)
                    {
                        resolvedDirection = toAimPoint.normalized;
                    }
                }

                SpellContext resolvedContext = new SpellContext
                {
                    // where the shape starts from after lob resolves
                    origin = impactPoint,

                    // where the player is currently aiming when the projectile resolves
                    aimPoint = resolvedAimPoint,

                    // direction from origin -> aimPoint
                    aimDirection = resolvedDirection
                };

                onResolve.Invoke(resolvedContext);
            }
        );
    }
}