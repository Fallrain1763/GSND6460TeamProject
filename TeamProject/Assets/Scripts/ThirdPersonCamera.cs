using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                 // Player root
    public Transform cameraPivot;            // empty object on player around chest/head height

    [Header("Position")]
    public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
    public float distance = 3.5f;
    public float shoulderOffset = 0.6f;

    [Header("Look")]
    public float mouseSensitivity = 2f;
    public float minPitch = -35f;
    public float maxPitch = 70f;

    [Header("Smoothing")]
    public float followSmoothness = 20f;

    float yaw;
    float pitch;

    public float Yaw => yaw;
    public Vector3 ForwardFlat
    {
        get
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            return flatForward;
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 startEuler = transform.eulerAngles;
        yaw = startEuler.y;
        pitch = startEuler.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 pivotPos = cameraPivot != null
            ? cameraPivot.position
            : target.position + pivotOffset;

        Vector3 desiredPos =
            pivotPos
            - rotation * Vector3.forward * distance
            + rotation * Vector3.right * shoulderOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            followSmoothness * Time.deltaTime
        );

        transform.rotation = rotation;
    }
}