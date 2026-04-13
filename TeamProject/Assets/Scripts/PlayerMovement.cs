using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 12f;

    [Header("Jump")]
    public float jumpForce = 7f;
    public bool isGrounded = true;

    [Header("References")]
    public ThirdPersonCamera cameraController;

    [Header("External Modifiers")]
    public float moveSpeedMultiplier = 1f;

    [Header("Mountain Collision")]
    [Tooltip("Slopes steeper than this (degrees) will block the player")]
    [Range(20f, 80f)] public float maxClimbAngle = 35f;

    Rigidbody rb;
    Vector3 moveDir;
    bool jumpPressed;

    // Set by OnCollisionStay, cleared each FixedUpdate
    bool    _touchingMountain;
    Vector3 _mountainNormal;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        if (ThirdPersonCamera.InputLocked)
        {
            moveDir     = Vector3.zero;
            jumpPressed = false;
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (cameraController == null)
        {
            moveDir = new Vector3(h, 0f, v).normalized;
        }
        else
        {
            Vector3 camForward = cameraController.ForwardFlat;
            Vector3 camRight   = Vector3.Cross(Vector3.up, camForward).normalized;
            moveDir = (camForward * v + camRight * h).normalized;
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            jumpPressed = true;
    }

    void FixedUpdate()
    {
        float actualMoveSpeed = moveSpeed * moveSpeedMultiplier;
        Vector3 desiredMove = moveDir * actualMoveSpeed;

        // If touching a steep mountain surface, strip out any component of movement
        // that points toward the mountain (dot product < 0 means moving into it)
        if (_touchingMountain)
        {
            Vector3 flatNormal = _mountainNormal;
            flatNormal.y = 0f;
            flatNormal.Normalize();

            float dot = Vector3.Dot(desiredMove.normalized, -flatNormal);
            if (dot > 0f)
            {
                // Remove the toward-mountain component from movement
                desiredMove -= flatNormal * -Vector3.Dot(desiredMove, -flatNormal);
            }

            // Also kill any upward velocity from slope riding
            if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            _touchingMountain = false;
        }

        rb.linearVelocity = new Vector3(desiredMove.x, rb.linearVelocity.y, desiredMove.z);

        if (jumpPressed)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            isGrounded  = false;
            jumpPressed = false;
        }

        if (cameraController != null)
        {
            Vector3 lookDir = cameraController.ForwardFlat;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                rb.MoveRotation(Quaternion.Slerp(
                    rb.rotation,
                    targetRot,
                    rotationSpeed * Time.fixedDeltaTime
                ));
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Mountain")) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            float angle = Vector3.Angle(contact.normal, Vector3.up);
            if (angle < maxClimbAngle) continue;

            _touchingMountain = true;
            _mountainNormal   = contact.normal;
            break;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }

    public void SetMoveSpeedMultiplier(float multiplier)
    {
        moveSpeedMultiplier = Mathf.Max(0f, multiplier);
    }
}