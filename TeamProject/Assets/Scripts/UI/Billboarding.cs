using UnityEngine;

public class Billboarding : MonoBehaviour
{
    public Transform cam;
    public Vector3 offset;
    private void Start()
    {
        cam = Camera.main.transform;
        offset = new Vector3(0f,0f,0f);
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position - cam.forward + offset);
    }
}
