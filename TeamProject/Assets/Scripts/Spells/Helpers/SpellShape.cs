using System.Collections.Generic;
using UnityEngine;

public static class SpellShape
{
    public static bool debugDraw = true;
    public static float debugDuration = 0.2f;

    // -------------------------
    // BURST
    // -------------------------
    public static Collider[] Burst(Vector3 center, float radius, LayerMask targetLayers)
    {
        if (debugDraw)
            DrawSphere(center, radius);

        return Physics.OverlapSphere(center, radius, targetLayers);
    }

    // -------------------------
    // EMANATION
    // -------------------------
    public static Collider[] Emanation(Vector3 origin, float radius, LayerMask targetLayers)
    {
        if (debugDraw)
            DrawSphere(origin, radius);

        return Physics.OverlapSphere(origin, radius, targetLayers);
    }

    // -------------------------
    // LINE
    // -------------------------
    public static Collider[] Line(Vector3 start, Vector3 direction, float length, float radius, LayerMask targetLayers)
    {
        Vector3 end = start + direction.normalized * length;

        if (debugDraw)
            DrawCapsule(start, end, radius);

        return Physics.OverlapCapsule(start, end, radius, targetLayers);
    }

    // -------------------------
    // CONE
    // -------------------------
    /*
    public static Collider[] Cone(Vector3 origin, Vector3 direction, float range, float angleDegrees, LayerMask targetLayers)
    {
        Collider[] possibleHits = Physics.OverlapSphere(origin, range, targetLayers);
        List<Collider> validHits = new List<Collider>();

        Vector3 forward = direction.normalized;
        float halfAngle = angleDegrees * 0.5f;

        foreach (Collider hit in possibleHits)
        {
            Vector3 toTarget = hit.transform.position - origin;

            if (toTarget.sqrMagnitude <= 0.001f)
                continue;

            float angleToTarget = Vector3.Angle(forward, toTarget.normalized);

            if (angleToTarget <= halfAngle)
            {
                validHits.Add(hit);
            }
        }

        if (debugDraw)
            DrawCone(origin, forward, range, angleDegrees);

        return validHits.ToArray();
    }
    */

    // =========================================================
    // DEBUG DRAW HELPERS
    // =========================================================

    static void DrawSphere(Vector3 center, float radius)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = center;
        marker.transform.localScale = Vector3.one * radius * 2f;

        CleanupMarker(marker);
    }

    static void DrawCapsule(Vector3 start, Vector3 end, float radius)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        Vector3 center = (start + end) * 0.5f;
        float height = Vector3.Distance(start, end);

        marker.transform.position = center;
        marker.transform.up = (end - start).normalized;

        // Unity cylinder is height = scale.y * 2
        marker.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);

        CleanupMarker(marker);
    }

    /*
    static void DrawCone(Vector3 origin, Vector3 forward, float range, float angle)
    {
        int steps = 12;
        float halfAngle = angle * 0.5f;

        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)(steps - 1);
            float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, t);

            Vector3 dir = Quaternion.AngleAxis(currentAngle, Vector3.up) * forward;
            Debug.DrawRay(origin, dir * range, Color.yellow, debugDuration);
        }

        // center line
        Debug.DrawRay(origin, forward * range, Color.red, debugDuration);
    }
    */

    static void CleanupMarker(GameObject marker)
    {
        Collider col = marker.GetComponent<Collider>();
        if (col != null)
            Object.Destroy(col);

        Object.Destroy(marker, debugDuration);
    }
}