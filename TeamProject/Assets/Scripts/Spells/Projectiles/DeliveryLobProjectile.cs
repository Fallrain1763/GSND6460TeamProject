using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class DeliveryLobProjectile : MonoBehaviour
{
    Rigidbody rb;
    Action<Vector3> onResolve;

    bool hasResolved = false;
    public GameObject explosionPrefab;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(
        Vector3 aimDirection,
        float launchSpeed,
        float upwardArc,
        Action<Vector3> resolveCallback)
    {
        onResolve = resolveCallback;

        rb.useGravity = true;
        rb.linearVelocity = aimDirection.normalized * launchSpeed + Vector3.up * upwardArc;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasResolved) return;

        hasResolved = true;

        Vector3 resolvePoint = collision.contacts.Length > 0
            ? collision.contacts[0].point
            : transform.position;

        onResolve?.Invoke(resolvePoint);

        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}