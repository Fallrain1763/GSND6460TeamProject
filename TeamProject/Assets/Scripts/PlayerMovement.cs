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

    Rigidbody rb;
    Vector3 moveDir;
    bool jumpPressed;

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
        Vector3 velocity = new Vector3(
            moveDir.x * moveSpeed,
            rb.linearVelocity.y,
            moveDir.z * moveSpeed
        );

        rb.linearVelocity = velocity;

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

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }
}