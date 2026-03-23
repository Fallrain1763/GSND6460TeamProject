using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    Rigidbody rb;
    Vector3 moveDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Prevent capsule from tipping over on collision
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                         | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        // Normalize to prevent faster diagonal movement
        moveDir = new Vector3(h, 0f, v).normalized;
    }

    void FixedUpdate()
    {
        // Preserve Y velocity (gravity), only override XZ
        rb.linearVelocity = new Vector3(
            moveDir.x * moveSpeed,
            rb.linearVelocity.y,
            moveDir.z * moveSpeed
        );
    }
}