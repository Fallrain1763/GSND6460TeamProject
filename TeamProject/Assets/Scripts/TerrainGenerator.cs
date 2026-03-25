using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    // ── Terrain ───────────────────────────────────────────────────────────────
    [Header("Terrain Reference")]
    public Terrain terrain;

    [Header("Terrain Size")]
    public int terrainWidth  = 512;
    public int terrainLength = 512;
    public int terrainHeight = 60;

    // ── Ridges ────────────────────────────────────────────────────────────────
    [Header("Diagonal Ridges")]
    [Range(1, 10)] public int   ridgeCount          = 4;
    [Range(0.1f, 1f)]  public float ridgeHeightFraction = 0.55f;
    [Range(0.01f, 0.25f)] public float ridgeWidth    = 0.07f;
    [Range(20f, 70f)]  public float ridgeAngleDeg    = 45f;
    [Range(0f, 0.05f)] public float ridgeWobble      = 0.015f;

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
    [Tooltip("How many trees to scatter on the flat ground")]
    public int treeCount = 200;

    [Tooltip("Max steepness (degrees) for tree placement")]
    [Range(5f, 40f)] public float treeMaxSlope = 15f;

    [Tooltip("Max normalised height for tree placement — keeps trees off ridge tops")]
    [Range(0f, 0.4f)] public float treeMaxNormHeight = 0.22f;

    [Tooltip("Min/max tree height in world units")]
    public Vector2 treeHeightRange = new Vector2(4f, 9f);

    [Tooltip("Trunk color")]
    public Color trunkColor = new Color(0.38f, 0.24f, 0.12f);

    [Tooltip("Foliage colors — picked randomly per tree for variety")]
    public Color[] foliageColors = new Color[]
    {
        new Color(0.13f, 0.45f, 0.13f),
        new Color(0.17f, 0.55f, 0.17f),
        new Color(0.10f, 0.38f, 0.18f),
        new Color(0.22f, 0.50f, 0.12f),
    };

    // ── Private ───────────────────────────────────────────────────────────────
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
            Debug.LogError("Assign a Terrain in the Inspector.");
            return;
        }

        _rng = new System.Random(seed);
        _td  = terrain.terrainData;

        ResizeTerrain();
        BuildHeightmap();
        ApplyTextures();
        SpawnProceduralTrees();

        terrain.Flush();
        Debug.Log("Generation complete.");
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
        int   res      = _td.heightmapResolution;
        float angleRad = ridgeAngleDeg * Mathf.Deg2Rad;
        float cos      = Mathf.Cos(angleRad);
        float sin      = Mathf.Sin(angleRad);

        float[] wobbleOffsets = new float[ridgeCount];
        float[] ridgeCentres  = new float[ridgeCount];
        for (int r = 0; r < ridgeCount; r++)
        {
            wobbleOffsets[r] = (float)_rng.NextDouble() * 1000f;
            ridgeCentres[r]  = (r + 0.5f) / ridgeCount
                              + ((float)_rng.NextDouble() - 0.5f) * (0.5f / ridgeCount);
        }

        float gnOffX = (float)_rng.NextDouble() * 5000f;
        float gnOffZ = (float)_rng.NextDouble() * 5000f;

        float[,] h = new float[res, res];
        for (int zi = 0; zi < res; zi++)
        {
            for (int xi = 0; xi < res; xi++)
            {
                float nx   = (float)xi / (res - 1);
                float nz   = (float)zi / (res - 1);
                float diag = nx * cos + nz * sin;
                float gn   = groundNoise * Mathf.PerlinNoise(
                                 nx * terrainWidth  / groundNoiseScale + gnOffX,
                                 nz * terrainLength / groundNoiseScale + gnOffZ);

                float height = groundHeight + gn;

                for (int r = 0; r < ridgeCount; r++)
                {
                    float perp   = -nx * sin + nz * cos;
                    float wobble = ridgeWobble * (Mathf.PerlinNoise(perp * 3f + wobbleOffsets[r], 0f) - 0.5f);
                    float t      = diag - (ridgeCentres[r] + wobble);
                    float ridgeH = Mathf.Exp(-t * t / (ridgeWidth * ridgeWidth) * 3f) * ridgeHeightFraction;
                    height       = Mathf.Max(height, ridgeH);
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
        // Destroy old tree container if regenerating
        if (_treeParent != null) DestroyImmediate(_treeParent);
        _treeParent = new GameObject("ProceduralTrees");
        _treeParent.transform.SetParent(terrain.transform);

        Vector3 terrainPos  = terrain.transform.position;
        int     attempts    = treeCount * 12;
        int     placed      = 0;

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
            float worldY = terrainPos.y + _td.GetInterpolatedHeight(nx, nz);

            float treeH = treeHeightRange.x
                        + (float)_rng.NextDouble() * (treeHeightRange.y - treeHeightRange.x);

            // Random foliage color
            Color foliage = foliageColors.Length > 0
                ? foliageColors[_rng.Next(0, foliageColors.Length)]
                : new Color(0.15f, 0.50f, 0.15f);

            // Random Y rotation
            float yRot = (float)_rng.NextDouble() * 360f;

            BuildTree(new Vector3(worldX, worldY, worldZ), treeH, foliage, yRot);
            placed++;
        }

        Debug.Log($"Spawned {placed} procedural trees.");
    }

    /// Builds one low-poly tree as a child of _treeParent.
    /// Structure: flat-shaded trunk (cylinder) + 3-tier stacked cones (foliage).
    void BuildTree(Vector3 basePos, float totalHeight, Color foliageColor, float yRot)
    {
        GameObject root = new GameObject("Tree");
        root.transform.SetParent(_treeParent.transform);
        root.transform.position = basePos;
        root.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

        float trunkH   = totalHeight * 0.35f;
        float trunkR   = totalHeight * 0.045f;
        float canopyH  = totalHeight * 0.75f;   // total height of canopy stack

        // ── Trunk ────────────────────────────────────────────────────────────
        GameObject trunk = new GameObject("Trunk");
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localPosition = new Vector3(0f, trunkH * 0.5f, 0f);

        var tmf = trunk.AddComponent<MeshFilter>();
        var tmr = trunk.AddComponent<MeshRenderer>();
        tmf.sharedMesh = MakeCylinder(trunkR, trunkH, 6);
        tmr.sharedMaterial = FlatMaterial(trunkColor);

        // ── Foliage: 3 stacked cones, each slightly smaller and higher ────────
        int   tiers    = 3;
        float baseRad  = totalHeight * 0.32f;
        float coneH    = canopyH     * 0.55f;
        float overlap  = coneH       * 0.30f;   // how much each tier overlaps the one below

        for (int i = 0; i < tiers; i++)
        {
            float t       = (float)i / tiers;
            float radius  = Mathf.Lerp(baseRad, baseRad * 0.45f, t);
            float height  = Mathf.Lerp(coneH,   coneH   * 0.75f, t);
            float yOff    = trunkH * 0.85f + i * (coneH - overlap);

            // Slightly vary foliage color per tier for depth
            Color tierColor = foliageColor * Mathf.Lerp(1f, 1.18f, t);
            tierColor.a = 1f;

            GameObject cone = new GameObject($"Foliage_{i}");
            cone.transform.SetParent(root.transform, false);
            cone.transform.localPosition = new Vector3(0f, yOff, 0f);

            var cmf = cone.AddComponent<MeshFilter>();
            var cmr = cone.AddComponent<MeshRenderer>();
            cmf.sharedMesh = MakeCone(radius, height, 7);
            cmr.sharedMaterial = FlatMaterial(tierColor);
        }
    }

    // ── Mesh builders ─────────────────────────────────────────────────────────

    /// Upright cylinder centred at local origin, flat-shaded (duplicated verts per face).
    Mesh MakeCylinder(float radius, float height, int sides)
    {
        var verts  = new List<Vector3>();
        var norms  = new List<Vector3>();
        var tris   = new List<int>();

        float half = height * 0.5f;

        for (int i = 0; i < sides; i++)
        {
            float a0 = (float)i       / sides * Mathf.PI * 2f;
            float a1 = (float)(i + 1) / sides * Mathf.PI * 2f;

            Vector3 b0 = new Vector3(Mathf.Cos(a0) * radius, -half, Mathf.Sin(a0) * radius);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * radius, -half, Mathf.Sin(a1) * radius);
            Vector3 t0 = new Vector3(Mathf.Cos(a0) * radius,  half, Mathf.Sin(a0) * radius);
            Vector3 t1 = new Vector3(Mathf.Cos(a1) * radius,  half, Mathf.Sin(a1) * radius);

            Vector3 faceN = Vector3.Cross(t1 - b0, b1 - b0).normalized;

            int base_ = verts.Count;
            verts.AddRange(new[] { b0, b1, t0, t1 });
            norms.AddRange(new[] { faceN, faceN, faceN, faceN });
            tris.AddRange(new[] { base_, base_+2, base_+1, base_+1, base_+2, base_+3 });
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    /// Cone with base at local origin, tip pointing up, flat-shaded.
    Mesh MakeCone(float radius, float height, int sides)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris  = new List<int>();

        Vector3 tip = new Vector3(0f, height, 0f);

        for (int i = 0; i < sides; i++)
        {
            float a0 = (float)i       / sides * Mathf.PI * 2f;
            float a1 = (float)(i + 1) / sides * Mathf.PI * 2f;

            Vector3 b0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);

            Vector3 faceN = Vector3.Cross(b1 - b0, tip - b0).normalized;

            int base_ = verts.Count;
            verts.AddRange(new[] { b0, b1, tip });
            norms.AddRange(new[] { faceN, faceN, faceN });
            tris.AddRange(new[] { base_, base_+1, base_+2 });
        }

        // Bottom cap
        Vector3 centre = Vector3.zero;
        Vector3 capN   = Vector3.down;
        for (int i = 0; i < sides; i++)
        {
            float a0 = (float)i       / sides * Mathf.PI * 2f;
            float a1 = (float)(i + 1) / sides * Mathf.PI * 2f;
            Vector3 b0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
            int base_ = verts.Count;
            verts.AddRange(new[] { centre, b1, b0 });
            norms.AddRange(new[] { capN, capN, capN });
            tris.AddRange(new[] { base_, base_+1, base_+2 });
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    /// Flat-shaded unlit material using a solid color.
    Material FlatMaterial(Color color)
    {
        // Use the Unlit/Color shader for a perfectly flat look (no lighting artifacts)
        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        return mat;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static int NextPow2(int v) { int p = 1; while (p < v) p <<= 1; return p; }
}