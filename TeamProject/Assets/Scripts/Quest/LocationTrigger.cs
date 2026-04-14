using UnityEngine;

public class LocationTrigger : MonoBehaviour
{
    [Tooltip("Must exactly match the location name used in quest generation (e.g. 'Location1')")]
    public string locationName;

    [Header("Light Beam")]
    public GameObject lightBeam;        // Assign a child GameObject with your beam visuals
    public float beamHeight = 20f;      // How tall the beam is
    public Color beamColor = new Color(1f, 0.9f, 0.3f, 0.4f);

    MeshRenderer beamRenderer;

    void Start()
    {
        if (lightBeam == null)
            lightBeam = CreateBeam();

        beamRenderer = lightBeam.GetComponent<MeshRenderer>();
        SetBeamVisible(false);
    }

    public void SetBeamVisible(bool visible)
    {
        if (lightBeam != null)
            lightBeam.SetActive(visible);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        SetBeamVisible(false);
        QuestManager.Instance.ReportLocationReached(locationName);
    }

    // Procedurally creates a beam using a cylinder if none is assigned
    GameObject CreateBeam()
    {
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = "LightBeam";
        beam.transform.SetParent(transform);

        // Position: centered horizontally, rising upward from ground
        beam.transform.localPosition = new Vector3(0f, beamHeight / 2f, 0f);
        beam.transform.localScale    = new Vector3(0.3f, beamHeight / 2f, 0.3f);
        beam.transform.localRotation = Quaternion.identity;

        // Remove collider so it doesn't interfere with triggers
        Destroy(beam.GetComponent<Collider>());

        // Create a transparent emissive material
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 1);          // Transparent
        mat.SetFloat("_Blend", 0);            // Alpha blend
        mat.SetFloat("_AlphaClip", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.color = beamColor;
        mat.SetColor("_EmissionColor", beamColor * 2f);
        mat.EnableKeyword("_EMISSION");

        beam.GetComponent<MeshRenderer>().material = mat;
        return beam;
    }
}