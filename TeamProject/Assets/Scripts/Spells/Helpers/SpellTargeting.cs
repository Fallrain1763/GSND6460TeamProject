using UnityEngine;

public static class SpellTargeting
{
    public static Vector3 ClampPointToRange(Vector3 origin, Vector3 targetPoint, float maxRange)
    {
        Vector3 offset = targetPoint - origin;
        float distance = offset.magnitude;

        if (distance <= 0.001f)
            return origin;

        Vector3 direction = offset / distance;
        float clampedDistance = Mathf.Min(distance, maxRange);

        return origin + direction * clampedDistance;
    }
}