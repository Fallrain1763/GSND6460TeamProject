using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Procedural terrain generator.
/// Finite ridges that terminate in all 4 directions, varying angles per ridge.
///
/// SETUP:
/// 1. Create an empty GameObject → attach this script
/// 2. Create a Terrain (GameObject > 3D Object > Terrain) → assign to "terrain" field
/// 3. Right-click this component → "Generate Terrain"  (or just press Play)
/// </summary>
[ExecuteInEditMode]
public class ProceduralTerrainGenerator : MonoBehaviour
{
    // ── Terrain ───────────────────────────────────────────────────────────────
    [Header("Terrain Reference")]
    public Terrain terrain;

    [Header("Terrain Size")]
    public int terrainWidth  = 512;
    public int terrainLength = 512;
    public int terrainHeight = 60;

    // ── Ridges ────────────────────────────────────────────────────────────────
    [Header("Ridges")]

    [Tooltip("How many ridges to scatter across the terrain")]
    [Range(1, 20)]
    public int ridgeCount = 8;

    [Tooltip("Height of each ridge as a fraction of terrainHeight")]
    [Range(0.1f, 1f)]
    public float ridgeHeightFraction = 0.50f;

    [Tooltip("How wide/smooth each ridge is — higher = broader hill")]
    [Range(0.02f, 0.35f)]
    public float ridgeWidth = 0.08f;

    [Tooltip("Base angle for ridges (degrees). Each ridge is randomized around this.")]
    [Range(0f, 90f)]
    public float ridgeBaseAngle = 45f;

    [Tooltip("How much each ridge's angle can deviate from the base angle (degrees)")]
    [Range(0f, 60f)]
    public float ridgeAngleVariance = 25f;

    [Tooltip("Length of each ridge as a fraction of the terrain diagonal (0=short, 1=full)")]
    [Range(0.1f, 1.0f)]
    public float ridgeLengthFraction = 0.55f;

    [Tooltip("How much ridge length varies per ridge")]
    [Range(0f, 0.4f)]
    public float ridgeLengthVariance = 0.2f;

    [Tooltip("Waviness along the ridge spine")]
    [Range(0f, 0.06f)]
    public float ridgeWobble = 0.02f;

    // ── Ground ────────────────────────────────────────────────────────────────
    [Header("Flat Ground")]
    [Range(0f, 0.15f)] public float groundHeight     = 0.04f;
    [Range(0f, 0.03f)] public float groundNoise      = 0.012f;
    public float groundNoiseScale = 200f;

    // ── Seed ──────────────────────────────────────────────────────────────────
    [Header("Randomization")]
    public int  seed                = 42;
    public bool randomizeSeedOnPlay = false;

    // ── Textures ──────────────────────────────────────────────────────────────
    [Header("Texturing")]
    public Texture2D grassTexture;
    public Texture2D dirtTexture;
    [Range(5f, 45f)]  public float slopeThreshold = 18f;
    [Range(1f, 20f)]  public float slopeBlendRange = 8f;

    // ── Trees ─────────────────────────────────────────────────────────────────
    [Header("Procedural Trees")]
    public int treeCount = 200;
    [Range(5f, 40f)]  public float treeMaxSlope      = 15f;
    [Range(0f, 0.4f)] public float treeMaxNormHeight = 0.22f;
    public Vector2 treeHeightRange = new Vector2(4f, 9f);
    public Color   trunkColor      = new Color(0.38f, 0.24f, 0.12f);
    public Color[] foliageColors   = new Color[]
    {
        new Color(0.13f, 0.45f, 0.13f),
        new Color(0.17f, 0.55f, 0.17f),
        new Color(0.10f, 0.38f, 0.18f),
        new Color(0.22f, 0.50f, 0.12f),
    };

    // ── Private ───────────────────────────────────────────────────────────────

    // Describes one finite ridge
    struct RidgeData
    {
        public Vector2 centre;      // normalised (0-1) centre position on terrain
        public float   angle;       // radians, direction the ridge runs along
        public float   halfLen;     // half-length in normalised space
        public float   wobbleOff;   // perlin offset for wobble
    }

    private TerrainData   _td;
    private System.Random _rng;
    private GameObject    _treeParent;

    // =========================================================================

    void Start()
    {
        if (randomizeSeedOnPlay) seed = Random.Range(0, 99999);
        GenerateTerrain();
    }

    [ContextMenu("Generate Terrain")]
    public void GenerateTerrain()
    {
        if (terrain == null)
        {
            Debug.LogError("[ProceduralTerrain] Assign a Terrain in the Inspector.");
            return;
        }

        _rng = new System.Random(seed);
        _td  = terrain.terrainData;

        ResizeTerrain();
        BuildHeightmap();
        ApplyTextures();
        SpawnProceduralTrees();

        terrain.Flush();
        Debug.Log("[ProceduralTerrain] Generation complete.");
    }

    // ── Terrain size ──────────────────────────────────────────────────────────

    void ResizeTerrain()
    {
        int res = NextPow2(Mathf.Max(terrainWidth, terrainLength)) + 1;
        _td.heightmapResolution = res;
        _td.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
    }

    // ── Heightmap ─────────────────────────────────────────────────────────────

    void BuildHeightmap()
    {
        // Build ridge descriptors
        RidgeData[] ridges = new RidgeData[ridgeCount];
        float diagLen = Mathf.Sqrt(2f); // max possible diagonal in normalised space
        for (int r = 0; r < ridgeCount; r++)
        {
            float angleRad = (ridgeBaseAngle + ((float)_rng.NextDouble() * 2f - 1f) * ridgeAngleVariance)
                           * Mathf.Deg2Rad;

            float lenFrac  = ridgeLengthFraction
                           + ((float)_rng.NextDouble() * 2f - 1f) * ridgeLengthVariance;
            lenFrac = Mathf.Clamp(lenFrac, 0.05f, 1.0f);

            ridges[r] = new RidgeData
            {
                // Random centre, biased away from the very edges
                centre    = new Vector2(
                                0.1f + (float)_rng.NextDouble() * 0.8f,
                                0.1f + (float)_rng.NextDouble() * 0.8f),
                angle     = angleRad,
                halfLen   = lenFrac * diagLen * 0.5f,
                wobbleOff = (float)_rng.NextDouble() * 1000f,
            };
        }

        float gnOffX = (float)_rng.NextDouble() * 5000f;
        float gnOffZ = (float)_rng.NextDouble() * 5000f;

        int res = _td.heightmapResolution;
        float[,] h = new float[res, res];

        for (int zi = 0; zi < res; zi++)
        {
            for (int xi = 0; xi < res; xi++)
            {
                float nx = (float)xi / (res - 1);
                float nz = (float)zi / (res - 1);

                // Subtle base ground noise
                float gn = groundNoise * Mathf.PerlinNoise(
                    nx * terrainWidth  / groundNoiseScale + gnOffX,
                    nz * terrainLength / groundNoiseScale + gnOffZ);

                float height = groundHeight + gn;

                foreach (var ridge in ridges)
                {
                    float cos = Mathf.Cos(ridge.angle);
                    float sin = Mathf.Sin(ridge.angle);

                    // Vector from ridge centre to this point (normalised space)
                    float dx = nx - ridge.centre.x;
                    float dz = nz - ridge.centre.y;

                    // Project onto the ridge's local axes
                    float along = dx * cos + dz * sin;   // distance along ridge spine
                    float perp  = -dx * sin + dz * cos;  // distance perpendicular to spine

                    // -- Fade ends: smoothstep from 0 at ±halfLen to 1 at centre --
                    float tAlong = Mathf.Abs(along) / ridge.halfLen;
                    if (tAlong >= 1f) continue;   // outside ridge extent entirely

                    float endFade = 1f - Mathf.SmoothStep(0.5f, 1f, tAlong);

                    // -- Wobble: shift the perpendicular centre slightly --
                    float wobble = ridgeWobble * (Mathf.PerlinNoise(
                        along * 4f + ridge.wobbleOff, 0f) - 0.5f);

                    float perpOff = perp - wobble;

                    // -- Cross-section bell curve --
                    float ridgeH = Mathf.Exp(-perpOff * perpOff / (ridgeWidth * ridgeWidth) * 3f)
                                 * ridgeHeightFraction
                                 * endFade;

                    height = Mathf.Max(height, ridgeH);
                }

                h[zi, xi] = Mathf.Clamp01(height);
            }
        }

        _td.SetHeights(0, 0, h);
    }

    // ── Textures ──────────────────────────────────────────────────────────────

    void ApplyTextures()
    {
        _td.terrainLayers = new TerrainLayer[]
        {
            new TerrainLayer { diffuseTexture = grassTexture ?? SolidTex(new Color(0.18f, 0.52f, 0.15f)), tileSize = new Vector2(8,8) },
            new TerrainLayer { diffuseTexture = dirtTexture  ?? SolidTex(new Color(0.52f, 0.35f, 0.18f)), tileSize = new Vector2(6,6) }
        };

        int res = _td.alphamapResolution;
        float[,,] maps = new float[res, res, 2];
        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                float steep = _td.GetSteepness((float)x / res, (float)z / res);
                float dirt  = Mathf.Clamp01((steep - slopeThreshold) / slopeBlendRange);
                maps[z, x, 0] = 1f - dirt;
                maps[z, x, 1] = dirt;
            }
        _td.SetAlphamaps(0, 0, maps);
    }

    Texture2D SolidTex(Color c)
    {
        var t = new Texture2D(4, 4);
        var p = new Color[16];
        for (int i = 0; i < 16; i++) p[i] = c;
        t.SetPixels(p); t.Apply(); return t;
    }

    // ── Procedural Trees ──────────────────────────────────────────────────────

    void SpawnProceduralTrees()
    {
        // Destroy by name so stale trees from previous Play sessions are also cleaned up
        var existing = GameObject.Find("ProceduralTrees");
        if (existing != null) DestroyImmediate(existing);
        if (_treeParent != null) DestroyImmediate(_treeParent);
        _treeParent = new GameObject("ProceduralTrees");
        _treeParent.transform.SetParent(terrain.transform);

        Vector3 terrainPos = terrain.transform.position;
        int     placed     = 0;
        int     attempts   = treeCount * 12;

        for (int a = 0; a < attempts && placed < treeCount; a++)
        {
            float nx = (float)_rng.NextDouble();
            float nz = (float)_rng.NextDouble();

            float normH = _td.GetInterpolatedHeight(nx, nz) / _td.size.y;
            float steep = _td.GetSteepness(nx, nz);

            if (normH > treeMaxNormHeight) continue;
            if (steep > treeMaxSlope)      continue;

            float worldX = terrainPos.x + nx * terrainWidth;
            float worldZ = terrainPos.z + nz * terrainLength;
            // SampleHeight takes world-space XZ and returns the exact surface height
            float worldY = terrainPos.y + terrain.SampleHeight(new Vector3(worldX, 0f, worldZ));

            float treeH  = treeHeightRange.x + (float)_rng.NextDouble()
                         * (treeHeightRange.y - treeHeightRange.x);

            Color foliage = foliageColors.Length > 0
                ? foliageColors[_rng.Next(0, foliageColors.Length)]
                : new Color(0.15f, 0.50f, 0.15f);

            BuildTree(new Vector3(worldX, worldY, worldZ), treeH, foliage,
                      (float)_rng.NextDouble() * 360f);
            placed++;
        }

        Debug.Log($"[ProceduralTerrain] Spawned {placed} trees.");
    }

    void BuildTree(Vector3 basePos, float totalHeight, Color foliageColor, float yRot)
    {
        GameObject root = new GameObject("Tree");
        root.transform.SetParent(_treeParent.transform);
        root.transform.position = basePos;
        root.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

        float trunkH  = totalHeight * 0.35f;
        float trunkR  = totalHeight * 0.045f;
        float coneH   = totalHeight * 0.55f;
        float overlap = coneH       * 0.30f;
        float baseRad = totalHeight * 0.32f;

        // Trunk
        var trunk = new GameObject("Trunk");
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localPosition = new Vector3(0f, trunkH * 0.5f, 0f);
        trunk.AddComponent<MeshFilter>().sharedMesh = MakeCylinder(trunkR, trunkH, 6);
        trunk.AddComponent<MeshRenderer>().sharedMaterial = FlatMaterial(trunkColor);

        // 3 stacked cones
        for (int i = 0; i < 3; i++)
        {
            float t      = (float)i / 3;
            float radius = Mathf.Lerp(baseRad, baseRad * 0.45f, t);
            float height = Mathf.Lerp(coneH,   coneH   * 0.75f, t);
            float yOff   = trunkH * 0.85f + i * (coneH - overlap);

            Color tierColor = foliageColor * Mathf.Lerp(1f, 1.18f, t);
            tierColor.a = 1f;

            var cone = new GameObject($"Foliage_{i}");
            cone.transform.SetParent(root.transform, false);
            cone.transform.localPosition = new Vector3(0f, yOff, 0f);
            cone.AddComponent<MeshFilter>().sharedMesh = MakeCone(radius, height, 7);
            cone.AddComponent<MeshRenderer>().sharedMaterial = FlatMaterial(tierColor);
        }
    }

    // ── Mesh builders ─────────────────────────────────────────────────────────

    Mesh MakeCylinder(float radius, float height, int sides)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris  = new List<int>();
        float half = height * 0.5f;

        for (int i = 0; i < sides; i++)
        {
            float a0 = (float)i       / sides * Mathf.PI * 2f;
            float a1 = (float)(i + 1) / sides * Mathf.PI * 2f;
            Vector3 b0 = new Vector3(Mathf.Cos(a0) * radius, -half, Mathf.Sin(a0) * radius);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * radius, -half, Mathf.Sin(a1) * radius);
            Vector3 t0 = new Vector3(Mathf.Cos(a0) * radius,  half, Mathf.Sin(a0) * radius);
            Vector3 t1 = new Vector3(Mathf.Cos(a1) * radius,  half, Mathf.Sin(a1) * radius);
            Vector3 fn = Vector3.Cross(t1 - b0, b1 - b0).normalized;
            int b = verts.Count;
            verts.AddRange(new[] { b0, b1, t0, t1 });
            norms.AddRange(new[] { fn, fn, fn, fn });
            tris.AddRange(new[] { b, b+2, b+1, b+1, b+2, b+3 });
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds(); return mesh;
    }

    Mesh MakeCone(float radius, float height, int sides)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris  = new List<int>();
        Vector3 tip  = new Vector3(0f, height, 0f);
        Vector3 capN = Vector3.down;

        for (int i = 0; i < sides; i++)
        {
            float a0 = (float)i       / sides * Mathf.PI * 2f;
            float a1 = (float)(i + 1) / sides * Mathf.PI * 2f;
            Vector3 b0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
            Vector3 fn = Vector3.Cross(b1 - b0, tip - b0).normalized;
            int b = verts.Count;
            verts.AddRange(new[] { b0, b1, tip });
            norms.AddRange(new[] { fn, fn, fn });
            tris.AddRange(new[] { b, b+1, b+2 });
        }

        for (int i = 0; i < sides; i++)
        {
            float a0 = (float)i       / sides * Mathf.PI * 2f;
            float a1 = (float)(i + 1) / sides * Mathf.PI * 2f;
            Vector3 b0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
            int b = verts.Count;
            verts.AddRange(new[] { Vector3.zero, b1, b0 });
            norms.AddRange(new[] { capN, capN, capN });
            tris.AddRange(new[] { b, b+1, b+2 });
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds(); return mesh;
    }

    Material FlatMaterial(Color color)
    {
        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        return mat;
    }

    static int NextPow2(int v) { int p = 1; while (p < v) p <<= 1; return p; }
}