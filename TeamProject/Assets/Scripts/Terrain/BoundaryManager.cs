using UnityEngine;
using System.Collections;

/// <summary>
/// Creates invisible walls around the terrain with a fog/smoke deterrent effect.
/// When the player approaches a boundary, fog particles fade in to warn them.
/// The invisible wall prevents them from passing through.
///
/// SETUP:
/// 1. Create an empty GameObject, name it "BoundaryManager"
/// 2. Attach this script to it
/// 3. Set "Terrain Size" to match your total terrain dimensions
///    (default is 512x512 for two 256-wide quadrants side by side)
/// 4. Make sure your Player is tagged "Player"
/// </summary>
public class BoundaryManager : MonoBehaviour
{
    [Header("Terrain Bounds")]
    [Tooltip("Total width of all terrain (quadWidth * 2)")]
    public float totalWidth  = 512f;
    [Tooltip("Total length of all terrain (quadLength * 2)")]
    public float totalLength = 512f;
    [Tooltip("Height of the invisible walls")]
    public float wallHeight  = 60f;
    [Tooltip("Where the terrain starts in world space")]
    public Vector3 terrainOrigin = Vector3.zero;

    [Header("Fog Effect")]
    [Tooltip("How close the player needs to be to trigger fog (world units)")]
    public float fogTriggerDistance = 20f;
    [Tooltip("Fog particle color")]
    public Color fogColor = new Color(0.7f, 0.8f, 0.9f, 0.6f);
    [Tooltip("How many fog puff particles per wall")]
    public int particlesPerWall = 30;
    [Tooltip("How tall the fog wall is")]
    public float fogHeight = 12f;

    // ── Internals ─────────────────────────────────────────────────────────────

    private Transform          _player;
    private ParticleSystem[]   _wallFog = new ParticleSystem[4]; // N, S, E, W
    private BoxCollider[]      _walls   = new BoxCollider[4];
    private float[]            _wallDists = new float[4];

    // Wall indices
    const int NORTH = 0, SOUTH = 1, EAST = 2, WEST = 3;

    // =========================================================================

    void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
        else
            Debug.LogWarning("[Boundary] No GameObject tagged 'Player' found.");

        BuildWalls();
        BuildFogSystems();
    }

    void Update()
    {
        if (_player == null) return;
        UpdateFog();
    }

    // ── Build invisible walls ─────────────────────────────────────────────────

    void BuildWalls()
    {
        float cx = terrainOrigin.x + totalWidth  * 0.5f;
        float cz = terrainOrigin.z + totalLength * 0.5f;
        float hy = terrainOrigin.y + wallHeight  * 0.5f;
        float thickness = 2f;

        // North (high Z), South (low Z), East (high X), West (low X)
        CreateWall("Wall_North", new Vector3(cx, hy, terrainOrigin.z + totalLength),
                   new Vector3(totalWidth + thickness * 2f, wallHeight, thickness));
        CreateWall("Wall_South", new Vector3(cx, hy, terrainOrigin.z),
                   new Vector3(totalWidth + thickness * 2f, wallHeight, thickness));
        CreateWall("Wall_East",  new Vector3(terrainOrigin.x + totalWidth, hy, cz),
                   new Vector3(thickness, wallHeight, totalLength + thickness * 2f));
        CreateWall("Wall_West",  new Vector3(terrainOrigin.x, hy, cz),
                   new Vector3(thickness, wallHeight, totalLength + thickness * 2f));
    }

    void CreateWall(string wallName, Vector3 position, Vector3 size)
    {
        var go  = new GameObject(wallName);
        go.transform.SetParent(transform);
        go.transform.position = position;
        var col = go.AddComponent<BoxCollider>();
        col.size = size;
        // No renderer — invisible
    }

    // ── Build fog particle systems ─────────────────────────────────────────────

    void BuildFogSystems()
    {
        float cx = terrainOrigin.x + totalWidth  * 0.5f;
        float cz = terrainOrigin.z + totalLength * 0.5f;
        float inset = 1f; // place fog just inside the wall

        _wallFog[NORTH] = CreateFogWall("Fog_North",
            new Vector3(cx, terrainOrigin.y, terrainOrigin.z + totalLength - inset),
            new Vector3(totalWidth, fogHeight, 1f));

        _wallFog[SOUTH] = CreateFogWall("Fog_South",
            new Vector3(cx, terrainOrigin.y, terrainOrigin.z + inset),
            new Vector3(totalWidth, fogHeight, 1f));

        _wallFog[EAST] = CreateFogWall("Fog_East",
            new Vector3(terrainOrigin.x + totalWidth - inset, terrainOrigin.y, cz),
            new Vector3(1f, fogHeight, totalLength));

        _wallFog[WEST] = CreateFogWall("Fog_West",
            new Vector3(terrainOrigin.x + inset, terrainOrigin.y, cz),
            new Vector3(1f, fogHeight, totalLength));

        // Start all fog hidden
        foreach (var ps in _wallFog)
        {
            var emission = ps.emission;
            emission.rateOverTime = 0f;
        }
    }

    ParticleSystem CreateFogWall(string fogName, Vector3 position, Vector3 boxSize)
    {
        var go = new GameObject(fogName);
        go.transform.SetParent(transform);
        go.transform.position = position;

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop           = true;
        main.startLifetime  = 6f;
        main.startSpeed     = 0.3f;
        main.startSize      = new ParticleSystem.MinMaxCurve(6f, 12f);
        main.startColor     = fogColor;
        main.maxParticles   = particlesPerWall * 3;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Emit from a box shape matching the wall
        var shape       = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = boxSize;

        // Drift slowly upward and outward
        var vel             = ps.velocityOverLifetime;
        vel.enabled         = true;
        vel.space           = ParticleSystemSimulationSpace.Local;
        vel.y               = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);

        // Fade out over lifetime
        var col             = ps.colorOverLifetime;
        col.enabled         = true;
        var gradient        = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]  { new GradientColorKey(fogColor, 0f), new GradientColorKey(fogColor, 1f) },
            new GradientAlphaKey[]  { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.2f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = gradient;

        // Size grows over lifetime for a billowing effect
        var size            = ps.sizeOverLifetime;
        size.enabled        = true;
        var sizeCurve       = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f);
        sizeCurve.AddKey(1f, 1.5f);
        size.size           = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Remove collider from particle renderer
        var renderer        = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        // Use default particle material (always available)
        renderer.material   = new Material(Shader.Find("Particles/Standard Unlit")
                           ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply")
                           ?? Shader.Find("Sprites/Default"));
        renderer.material.color = fogColor;

        ps.Play();
        return ps;
    }

    // ── Update fog intensity based on player distance ─────────────────────────

    void UpdateFog()
    {
        float px = _player.position.x;
        float pz = _player.position.z;

        // Distance from each wall (negative = player is outside)
        _wallDists[NORTH] = (terrainOrigin.z + totalLength) - pz;
        _wallDists[SOUTH] = pz - terrainOrigin.z;
        _wallDists[EAST]  = (terrainOrigin.x + totalWidth)  - px;
        _wallDists[WEST]  = px - terrainOrigin.x;

        for (int i = 0; i < 4; i++)
        {
            float dist       = _wallDists[i];
            // Remap: at fogTriggerDistance → 0 emission, at 0 → full emission
            float t          = 1f - Mathf.Clamp01(dist / fogTriggerDistance);
            float rate       = Mathf.SmoothStep(0f, particlesPerWall, t);

            var emission     = _wallFog[i].emission;
            emission.rateOverTime = rate;
        }
    }
}