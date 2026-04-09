using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Four-biome terrain generator using 4 separate stitched Unity Terrains.
///
/// Layout (each terrain is half the total width/length):
///   [Top-Left]  Snowy Ridges  | [Top-Right]  Rocky Plateau
///   [Bot-Left]  Grassy Hills  | [Bot-Right]  Lake + Shore
///
/// SETUP:
/// 1. Create an empty GameObject → attach this script
/// 2. Right-click → "Generate Terrain", or press Play
/// </summary>
[ExecuteInEditMode]
public class ProceduralTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Size (each quadrant)")]
    public int quadWidth  = 256;
    public int quadLength = 256;
    public int quadHeight = 80;

    [Header("Ridge Settings")]
    [Range(2, 8)]        public int   ridgesPerBiome = 3;
    [Range(0.1f, 1f)]    public float ridgeHeight    = 0.55f;
    [Range(0.02f, 0.3f)] public float ridgeWidth     = 0.08f;
    [Range(0f, 0.06f)]   public float ridgeWobble    = 0.02f;
    [Range(0.2f, 0.7f)]  public float ridgeLength    = 0.45f;

    [Header("Lake Settings")]
    [Range(0.05f, 0.30f)] public float lakeRadius = 0.22f;
    [Range(0.02f, 0.12f)] public float shoreWidth = 0.07f;

    [Header("Plateau Settings")]
    [Range(0.15f, 0.5f)]  public float plateauHeight  = 0.28f;
    [Range(0.05f, 0.25f)] public float cliffSharpness = 0.12f;
    public int boulderCount = 30;

    [Header("Snow Settings")]
    [Range(0f, 0.5f)] public float snowLineNorm = 0.28f;

    [Header("Ground")]
    // groundLevel is the shared normalised height all 4 quadrant edges meet at
    [Range(0f, 0.12f)] public float groundLevel     = 0.05f;
    [Range(0f, 0.03f)] public float groundNoise     = 0.010f;
    public float groundNoiseScale = 120f;

    [Header("Randomization")]
    public int  seed                = 42;
    public bool randomizeSeedOnPlay = false;

    [Header("Trees")]
    public int     treeCount         = 80;
    [Range(5f, 40f)]  public float treeMaxSlope      = 18f;
    [Range(0f, 0.5f)] public float treeMaxNormHeight = 0.32f;
    public Vector2 treeHeightRange   = new Vector2(4f, 9f);
    public Color   trunkColor        = new Color(0.35f, 0.22f, 0.10f);
    public Color[] grassyFoliage     = { new Color(0.13f,0.45f,0.13f), new Color(0.17f,0.55f,0.17f) };
    public Color[] snowyFoliage      = { new Color(0.65f,0.80f,0.65f), new Color(0.75f,0.88f,0.72f) };

    [Header("Water")]
    public Texture2D waterTexture;
    public Color waterColor = new Color(0.15f, 0.42f, 0.72f, 1f);
    [Range(0.1f, 0.9f)] public float waterRadiusFraction = 0.75f; // fraction of lakeRadius the water disc fills

    [Header("Textures (optional)")]
    public Texture2D grassTex;
    public Texture2D dirtTex;
    public Texture2D sandTex;
    public Texture2D snowTex;
    public Texture2D rockTex;

    // ── internals ─────────────────────────────────────────────────────────────

    struct RidgeData
    {
        public Vector2 centre;
        public float   angle, halfLen, wobbleOff;
    }

    private System.Random _rng;
    private GameObject    _root;
    private GameObject    _propsRoot;

    const int BL = 0, BR = 1, TL = 2, TR = 3;

    // =========================================================================

    void Start()
    {
        if (randomizeSeedOnPlay) seed = Random.Range(0, 99999);
        GenerateTerrain();
    }

    [ContextMenu("Generate Terrain")]
    public void GenerateTerrain()
    {
        _rng = new System.Random(seed);
        CleanUp();

        _root      = new GameObject("Terrains");
        _propsRoot = new GameObject("TerrainProps");
        _root.transform.SetParent(transform);
        _propsRoot.transform.SetParent(transform);

        Terrain[] t = new Terrain[4];
        t[BL] = CreateQuadrant("Grassy_Hills",  new Vector3(0,         0, 0));
        t[BR] = CreateQuadrant("Lake_Shore",    new Vector3(quadWidth, 0, 0));
        t[TL] = CreateQuadrant("Snowy_Ridges",  new Vector3(0,         0, quadLength));
        t[TR] = CreateQuadrant("Rocky_Plateau", new Vector3(quadWidth, 0, quadLength));

        // Stitch neighbours (left, top, right, bottom)
        t[BL].SetNeighbors(null,    t[TL], t[BR], null);
        t[BR].SetNeighbors(t[BL],   t[TR], null,  null);
        t[TL].SetNeighbors(null,    null,  t[TR], t[BL]);
        t[TR].SetNeighbors(t[TL],   null,  null,  t[BR]);

        BuildGrassyHills (t[BL].terrainData);
        BuildLakeShore   (t[BR].terrainData);
        BuildSnowyRidges (t[TL].terrainData);
        BuildRockyPlateau(t[TR].terrainData);

        //                                biome           left            right           bottom          top
        // BL Grassy: snowy is above (top), no other neighbours
        PaintTerrain(t[BL].terrainData, BiomeType.Grassy,  null,           BiomeType.Lake,  null,           BiomeType.Snowy);
        // BR Lake: grassy is left, rocky is above (top)
        PaintTerrain(t[BR].terrainData, BiomeType.Lake,    BiomeType.Grassy, null,          null,           BiomeType.Rocky);
        PaintTerrain(t[TL].terrainData, BiomeType.Snowy,   null,           null,           null,           null);
        PaintTerrain(t[TR].terrainData, BiomeType.Rocky,   BiomeType.Snowy, null,          null,           null);

        foreach (var terrain in t) terrain.Flush();

        SpawnTrees(t[BL], grassyFoliage);
        SpawnTrees(t[TL], snowyFoliage);
        SpawnTrees(t[BR], grassyFoliage, avoidLakeCentre: true);
        SpawnBoulders(t[TR]);
        SpawnWaterPlane(t[BR]);

        Debug.Log("[Terrain] Done.");
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    void CleanUp()
    {
        foreach (var name in new[]{"Terrains","TerrainProps"})
        {
            var go = GameObject.Find(name);
            if (go != null) DestroyImmediate(go);
        }
        if (_root      != null) DestroyImmediate(_root);
        if (_propsRoot != null) DestroyImmediate(_propsRoot);
    }

    // ── Create quadrant ───────────────────────────────────────────────────────

    Terrain CreateQuadrant(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_root.transform);
        go.transform.position = pos;

        var td  = new TerrainData();
        int res = NextPow2(Mathf.Max(quadWidth, quadLength)) + 1;
        td.heightmapResolution = res;
        td.alphamapResolution  = Mathf.Min(res - 1, 512);
        td.size = new Vector3(quadWidth, quadHeight, quadLength);
        td.name = name + "_Data";

#if UNITY_EDITOR
        string folder = "Assets/GeneratedTerrains";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "GeneratedTerrains");
        string path = $"{folder}/{name}_Data.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(td, path);
#endif

        var terrain  = go.AddComponent<Terrain>();
        var col      = go.AddComponent<TerrainCollider>();
        terrain.terrainData = td;
        col.terrainData     = td;

        var shader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        terrain.materialTemplate = shader != null ? new Material(shader) : null;

        return terrain;
    }

    // =========================================================================
    // HEIGHTMAPS
    // =========================================================================

    // Ridges are confined to the INNER 60% of each quadrant (0.2 – 0.8)
    // so they never reach the shared edges, preventing the cut-off look.
    // All quadrants use the same groundLevel as their minimum height so
    // the seams between quadrants are flush.

    void BuildGrassyHills(TerrainData td)
    {
        var ridges = BuildRidges(ridgesPerBiome, 0.20f, 0.80f);
        ApplyHeights(td, (nx, nz, gn) => RidgeHeight(nx, nz, ridges, groundLevel + gn));
    }

    void BuildSnowyRidges(TerrainData td)
    {
        var ridges = BuildRidges(ridgesPerBiome, 0.20f, 0.80f);
        // Snowy ridges are taller
        ApplyHeights(td, (nx, nz, gn) => Mathf.Min(1f, RidgeHeight(nx, nz, ridges, groundLevel + gn) * 1.35f));
    }

    void BuildLakeShore(TerrainData td)
    {
        // Completely flat — just groundLevel everywhere.
        // The sandy circle is painted via texture, no height changes.
        ApplyHeights(td, (nx, nz, gn) => groundLevel + gn);
    }

    void BuildRockyPlateau(TerrainData td)
    {
        ApplyHeights(td, (nx, nz, gn) => PlateauHeight(nx, nz, groundLevel + gn));
    }

    // Shared heightmap writer — ensures edges are clamped to groundLevel
    // so adjacent quadrants always meet at the same height.
    void ApplyHeights(TerrainData td, System.Func<float,float,float,float> heightFn)
    {
        float offX = (float)_rng.NextDouble() * 5000f;
        float offZ = (float)_rng.NextDouble() * 5000f;
        int   res  = td.heightmapResolution;
        var   h    = new float[res, res];

        for (int zi = 0; zi < res; zi++)
        for (int xi = 0; xi < res; xi++)
        {
            float nx = (float)xi / (res - 1);
            float nz = (float)zi / (res - 1);
            float gn = groundNoise * Mathf.PerlinNoise(
                nx * quadWidth  / groundNoiseScale + offX,
                nz * quadLength / groundNoiseScale + offZ);

            float val = heightFn(nx, nz, gn);

            // Blend edges back to groundLevel over the outer 8% of the quadrant
            // so all 4 quadrants meet seamlessly
            float edgeFade = EdgeFade(nx, nz, 0.08f);
            val = Mathf.Lerp(groundLevel, val, edgeFade);

            h[zi, xi] = Mathf.Clamp01(val);
        }

        td.SetHeights(0, 0, h);
    }

    // Returns 0 at the very edge, 1 beyond the fade band
    float EdgeFade(float nx, float nz, float band)
    {
        float fx = Mathf.Min(nx, 1f - nx) / band;
        float fz = Mathf.Min(nz, 1f - nz) / band;
        return Mathf.Clamp01(Mathf.Min(fx, fz));
    }

    // ── Height functions ──────────────────────────────────────────────────────

    float RidgeHeight(float nx, float nz, RidgeData[] ridges, float baseH)
    {
        float height = baseH;
        foreach (var r in ridges)
        {
            float cos = Mathf.Cos(r.angle), sin = Mathf.Sin(r.angle);
            float dx = nx - r.centre.x, dz = nz - r.centre.y;
            float along  = dx * cos + dz * sin;
            float perp   = -dx * sin + dz * cos;
            float tAlong = Mathf.Abs(along) / r.halfLen;
            if (tAlong >= 1f) continue;
            float endFade = 1f - Mathf.SmoothStep(0.5f, 1f, tAlong);
            float wobble  = ridgeWobble * (Mathf.PerlinNoise(along * 4f + r.wobbleOff, 0f) - 0.5f);
            float rH = Mathf.Exp(-(perp - wobble) * (perp - wobble) / (ridgeWidth * ridgeWidth) * 3f)
                     * ridgeHeight * endFade;
            height = Mathf.Max(height, rH);
        }
        return height;
    }

    float PlateauHeight(float nx, float nz, float baseH)
    {
        // Rocky plateau stays at the same height as all other quadrants.
        // Only adds subtle rock-surface noise — no elevation change.
        float noise = Mathf.PerlinNoise(nx * 12f + 100f, nz * 12f + 100f) * 0.015f
                    + Mathf.PerlinNoise(nx * 28f + 200f, nz * 28f + 200f) * 0.006f;
        return baseH + noise;
    }

    // Ridges centred in the inner band [innerMin, innerMax]
    RidgeData[] BuildRidges(int count, float innerMin, float innerMax)
    {
        var arr = new RidgeData[count];
        float diagLen = Mathf.Sqrt(2f);
        for (int r = 0; r < count; r++)
        {
            float angle   = (30f + (float)_rng.NextDouble() * 50f) * Mathf.Deg2Rad;
            float lenFrac = Mathf.Clamp(ridgeLength + ((float)_rng.NextDouble() * 2f - 1f) * 0.12f, 0.1f, 0.65f);
            float cx = innerMin + (float)_rng.NextDouble() * (innerMax - innerMin);
            float cz = innerMin + (float)_rng.NextDouble() * (innerMax - innerMin);
            arr[r] = new RidgeData
            {
                centre    = new Vector2(cx, cz),
                angle     = angle,
                halfLen   = lenFrac * diagLen * 0.5f,
                wobbleOff = (float)_rng.NextDouble() * 1000f,
            };
        }
        return arr;
    }

    // =========================================================================
    // TEXTURES
    // =========================================================================

    enum BiomeType { Grassy, Lake, Snowy, Rocky }

    // Border blend band width (fraction of quadrant)
    const float EDGE_BLEND = 0.05f;
    // How much Perlin noise warps the border line
    const float EDGE_WARP  = 0.05f;
    // Scale of the warp noise
    const float WARP_SCALE = 2f;

    // Neighbour texture blend pairs per edge:
    // Each quadrant blends toward the texture of its actual neighbour.
    // Layout:  BL=Grassy  BR=Lake
    //          TL=Snowy   TR=Rocky
    //
    // Shared borders:
    //   BL right  ↔ BR left   (Grassy ↔ Lake)   → blend grass into sand
    //   BL top    ↔ TL bottom (Grassy ↔ Snowy)  → blend grass into snow
    //   BR top    ↔ TR bottom (Lake   ↔ Rocky)  → blend sand into rock
    //   TL right  ↔ TR left   (Snowy  ↔ Rocky)  → blend snow into rock

    void PaintTerrain(TerrainData td, BiomeType biome,
                      BiomeType? leftNeighbour,  BiomeType? rightNeighbour,
                      BiomeType? bottomNeighbour, BiomeType? topNeighbour)
    {
        // Primary and secondary textures for this biome
        Texture2D texA, texB;
        Color colA, colB;
        float tileA, tileB;
        GetBiomeTextures(biome, out texA, out colA, out tileA, out texB, out colB, out tileB);

        // Neighbour blend textures (we use their primary texture as the blend target)
        Texture2D texLeft   = leftNeighbour   != null ? GetPrimaryTex(leftNeighbour.Value)   : null;
        Texture2D texRight  = rightNeighbour  != null ? GetPrimaryTex(rightNeighbour.Value)  : null;
        Texture2D texBottom = bottomNeighbour != null ? GetPrimaryTex(bottomNeighbour.Value) : null;
        Texture2D texTop    = topNeighbour    != null ? GetPrimaryTex(topNeighbour.Value)    : null;

        Color colLeft   = leftNeighbour   != null ? GetPrimaryColor(leftNeighbour.Value)   : Color.white;
        Color colRight  = rightNeighbour  != null ? GetPrimaryColor(rightNeighbour.Value)  : Color.white;
        Color colBottom = bottomNeighbour != null ? GetPrimaryColor(bottomNeighbour.Value) : Color.white;
        Color colTop    = topNeighbour    != null ? GetPrimaryColor(topNeighbour.Value)    : Color.white;

        // Build layer list: always A, B, then any unique neighbour textures
        var layerTextures = new List<Texture2D> { texA, texB };
        var layerColors   = new List<Color>     { colA, colB };
        var layerTiles    = new List<float>      { tileA, tileB };

        int idxLeft=-1, idxRight=-1, idxBottom=-1, idxTop=-1;

        int AddOrFind(Texture2D tex, Color col, float tile)
        {
            // Reuse existing slot if same texture
            for (int i = 0; i < layerTextures.Count; i++)
                if (layerTextures[i] == tex) return i;
            layerTextures.Add(tex ?? SolidTex(col));
            layerColors.Add(col);
            layerTiles.Add(tile);
            return layerTextures.Count - 1;
        }

        if (leftNeighbour   != null) idxLeft   = AddOrFind(texLeft,   colLeft,   tileA);
        if (rightNeighbour  != null) idxRight  = AddOrFind(texRight,  colRight,  tileA);
        if (bottomNeighbour != null) idxBottom = AddOrFind(texBottom, colBottom, tileA);
        if (topNeighbour    != null) idxTop    = AddOrFind(texTop,    colTop,    tileA);

        int numLayers = layerTextures.Count;
        td.terrainLayers = SaveNLayers(biome.ToString(), layerTextures, layerColors, layerTiles);

        int res = td.alphamapResolution;
        var map = new float[res, res, numLayers];
        Vector2 lakeCentre = new Vector2(0.5f, 0.5f);

        for (int z = 0; z < res; z++)
        for (int x = 0; x < res; x++)
        {
            float nx    = (float)x / (res - 1);
            float nz    = (float)z / (res - 1);
            float steep = td.GetSteepness(nx, nz);

            // Base A/B weights from biome logic
            float wB;
            switch (biome)
            {
                case BiomeType.Grassy: wB = Mathf.Clamp01((steep - 12f) / 10f); break;
                case BiomeType.Lake:
                    float dist = Vector2.Distance(new Vector2(nx, nz), lakeCentre);
                    // wB = sand weight — high near centre, zero beyond shore
                    wB = 1f - Mathf.Clamp01((dist - lakeRadius) / (shoreWidth * 2f));
                    break;
                case BiomeType.Snowy: wB = Mathf.Clamp01((steep - 10f) / 8f); break;
                case BiomeType.Rocky: wB = Mathf.Clamp01((steep - 15f) / 10f); break;
                default: wB = 0f; break;
            }

            float wA = 1f - wB;

            // Perlin warp offsets for each edge — makes the border curvy
            float warpL = EDGE_WARP * (Mathf.PerlinNoise(nz * WARP_SCALE + 10f, 0.1f) - 0.5f);
            float warpR = EDGE_WARP * (Mathf.PerlinNoise(nz * WARP_SCALE + 20f, 0.2f) - 0.5f);
            float warpB = EDGE_WARP * (Mathf.PerlinNoise(nx * WARP_SCALE + 30f, 0.3f) - 0.5f);
            float warpT = EDGE_WARP * (Mathf.PerlinNoise(nx * WARP_SCALE + 40f, 0.4f) - 0.5f);

            // Warped distances from each edge (0=at edge, 1=past blend band)
            float dLeft   = Mathf.Clamp01((nx + warpL) / EDGE_BLEND);
            float dRight  = Mathf.Clamp01(((1f - nx) + warpR) / EDGE_BLEND);
            float dBottom = Mathf.Clamp01((nz + warpB) / EDGE_BLEND);
            float dTop    = Mathf.Clamp01(((1f - nz) + warpT) / EDGE_BLEND);

            // Neighbour blend weights (SmoothStep for soft falloff)
            float wLeft   = idxLeft   >= 0 ? Mathf.SmoothStep(1f, 0f, dLeft)   : 0f;
            float wRight  = idxRight  >= 0 ? Mathf.SmoothStep(1f, 0f, dRight)  : 0f;
            float wBottom = idxBottom >= 0 ? Mathf.SmoothStep(1f, 0f, dBottom) : 0f;
            float wTop    = idxTop    >= 0 ? Mathf.SmoothStep(1f, 0f, dTop)    : 0f;

            float totalNeighbour = wLeft + wRight + wBottom + wTop;
            // Cap neighbour blend so biome textures always show in the centre
            totalNeighbour = Mathf.Min(totalNeighbour, 0.85f);
            float biomeScale = 1f - totalNeighbour;

            float[] w = new float[numLayers];
            w[0] = wA * biomeScale;
            w[1] = wB * biomeScale;
            if (idxLeft   >= 0) w[idxLeft]   += wLeft;
            if (idxRight  >= 0) w[idxRight]  += wRight;
            if (idxBottom >= 0) w[idxBottom] += wBottom;
            if (idxTop    >= 0) w[idxTop]    += wTop;

            // Normalize
            float sum = 0f;
            for (int l = 0; l < numLayers; l++) sum += w[l];
            for (int l = 0; l < numLayers; l++) map[z, x, l] = sum > 0f ? w[l] / sum : 0f;
        }

        td.SetAlphamaps(0, 0, map);
    }

    void GetBiomeTextures(BiomeType b,
        out Texture2D tA, out Color cA, out float sA,
        out Texture2D tB, out Color cB, out float sB)
    {
        switch (b)
        {
            case BiomeType.Lake:
                tA=grassTex; cA=new Color(0.18f,0.52f,0.15f); sA=8f;
                tB=sandTex;  cB=new Color(0.82f,0.74f,0.45f); sB=6f; return;
            case BiomeType.Snowy:
                tA=snowTex; cA=new Color(0.90f,0.93f,0.96f); sA=8f;
                tB=dirtTex; cB=new Color(0.50f,0.33f,0.16f); sB=6f; return;
            case BiomeType.Rocky:
                tA=rockTex; cA=new Color(0.48f,0.46f,0.44f); sA=5f;
                tB=dirtTex; cB=new Color(0.50f,0.33f,0.16f); sB=6f; return;
            default:
                tA=grassTex; cA=new Color(0.18f,0.52f,0.15f); sA=8f;
                tB=dirtTex;  cB=new Color(0.50f,0.33f,0.16f); sB=6f; return;
        }
    }

    Texture2D GetPrimaryTex(BiomeType b)
    {
        switch (b)
        {
            case BiomeType.Lake:   return grassTex ?? SolidTex(new Color(0.18f,0.52f,0.15f));
            case BiomeType.Snowy:  return snowTex  ?? SolidTex(new Color(0.90f,0.93f,0.96f));
            case BiomeType.Rocky:  return rockTex  ?? SolidTex(new Color(0.48f,0.46f,0.44f));
            default:               return grassTex ?? SolidTex(new Color(0.18f,0.52f,0.15f));
        }
    }

    Color GetPrimaryColor(BiomeType b)
    {
        switch (b)
        {
            case BiomeType.Lake:  return new Color(0.18f,0.52f,0.15f);
            case BiomeType.Snowy: return new Color(0.90f,0.93f,0.96f);
            case BiomeType.Rocky: return new Color(0.48f,0.46f,0.44f);
            default:              return new Color(0.18f,0.52f,0.15f);
        }
    }

    TerrainLayer[] SaveNLayers(string id, List<Texture2D> textures, List<Color> colors, List<float> tiles)
    {
        var layers = new TerrainLayer[textures.Count];
        for (int i = 0; i < textures.Count; i++)
            layers[i] = new TerrainLayer
            {
                diffuseTexture = textures[i] ?? SolidTex(colors[i]),
                tileSize       = Vector2.one * tiles[i]
            };

#if UNITY_EDITOR
        string folder = "Assets/GeneratedTerrains";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "GeneratedTerrains");

        for (int i = 0; i < layers.Length; i++)
        {
            string p = $"{folder}/Layer_{id}_{i}.terrainlayer";
            AssetDatabase.DeleteAsset(p);
            AssetDatabase.CreateAsset(layers[i], p);
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        for (int i = 0; i < layers.Length; i++)
        {
            string p = $"{folder}/Layer_{id}_{i}.terrainlayer";
            layers[i] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(p);
        }
#endif
        return layers;
    }

    Texture2D SolidTex(Color c)
    {
        var t = new Texture2D(4, 4);
        var p = new Color[16]; for (int i = 0; i < 16; i++) p[i] = c;
        t.SetPixels(p); t.Apply(); return t;
    }

    // =========================================================================
    // PROPS
    // =========================================================================

    void SpawnWaterPlane(Terrain t)
    {
        Vector3 pos          = t.transform.position;
        float   centreWorldX = pos.x + quadWidth  * 0.5f;
        float   centreWorldZ = pos.z + quadLength * 0.5f;
        float   surfaceY     = pos.y + t.SampleHeight(new Vector3(centreWorldX, 0f, centreWorldZ));
        float   wY           = surfaceY + 0.2f;
        float   radius       = lakeRadius * Mathf.Min(quadWidth, quadLength) * waterRadiusFraction;

        // Cylinder scaled very flat makes a reliable disc in all pipelines
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "WaterPlane";
        go.transform.SetParent(_propsRoot.transform);
        go.transform.position   = new Vector3(centreWorldX, wY, centreWorldZ);
        go.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
        DestroyImmediate(go.GetComponent<Collider>());

        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = waterColor;
        go.GetComponent<Renderer>().sharedMaterial = mat;

        Debug.Log($"[Terrain] Water cylinder at {go.transform.position}, diameter={radius*2f:F1}");
    }

    void SpawnTrees(Terrain t, Color[] palette, bool avoidLakeCentre = false)
    {
        Vector3 pos    = t.transform.position;
        var     td     = t.terrainData;
        int     placed = 0;

        for (int a = 0; a < treeCount * 12 && placed < treeCount; a++)
        {
            float nx = 0.1f + (float)_rng.NextDouble() * 0.8f;
            float nz = 0.1f + (float)_rng.NextDouble() * 0.8f;

            // Skip the sandy lake centre
            if (avoidLakeCentre)
            {
                float dist = Vector2.Distance(new Vector2(nx, nz), new Vector2(0.5f, 0.5f));
                if (dist < lakeRadius + shoreWidth * 2f) continue;
            }

            float normH = td.GetInterpolatedHeight(nx, nz) / td.size.y;
            float steep = td.GetSteepness(nx, nz);
            if (normH > treeMaxNormHeight) continue;
            if (steep > treeMaxSlope)      continue;

            float wX = pos.x + nx * quadWidth;
            float wZ = pos.z + nz * quadLength;
            float wY = pos.y + t.SampleHeight(new Vector3(wX, 0f, wZ));

            float h = treeHeightRange.x + (float)_rng.NextDouble() * (treeHeightRange.y - treeHeightRange.x);
            BuildTree(new Vector3(wX, wY, wZ), h, palette[_rng.Next(0, palette.Length)],
                      (float)_rng.NextDouble() * 360f);
            placed++;
        }
    }

    void SpawnBoulders(Terrain t)
    {
        Vector3 pos = t.transform.position;
        for (int i = 0; i < boulderCount; i++)
        {
            float nx = 0.1f + (float)_rng.NextDouble() * 0.8f;
            float nz = 0.1f + (float)_rng.NextDouble() * 0.8f;
            float wX = pos.x + nx * quadWidth;
            float wZ = pos.z + nz * quadLength;
            float wY = pos.y + t.SampleHeight(new Vector3(wX, 0f, wZ));
            float sz = 1.5f + (float)_rng.NextDouble() * 3.5f;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Boulder";
            go.transform.SetParent(_propsRoot.transform);
            go.transform.position   = new Vector3(wX, wY + sz * 0.3f, wZ);
            go.transform.localScale = new Vector3(sz*(0.8f+(float)_rng.NextDouble()*0.5f),
                                                  sz*(0.5f+(float)_rng.NextDouble()*0.4f),
                                                  sz*(0.8f+(float)_rng.NextDouble()*0.5f));
            go.transform.rotation = Quaternion.Euler(0f, (float)_rng.NextDouble()*360f, 0f);
            Color rc = new Color(0.42f+(float)_rng.NextDouble()*0.12f,
                                 0.40f+(float)_rng.NextDouble()*0.10f,
                                 0.37f+(float)_rng.NextDouble()*0.08f);
            go.GetComponent<Renderer>().sharedMaterial = FlatMat(rc);
        }
    }

    // =========================================================================
    // TREE BUILDER
    // =========================================================================

    void BuildTree(Vector3 basePos, float totalH, Color foliage, float yRot)
    {
        var root = new GameObject("Tree");
        root.transform.SetParent(_propsRoot.transform);
        root.transform.position = basePos;
        root.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

        float trunkH=totalH*0.35f, trunkR=totalH*0.045f;
        float coneH=totalH*0.55f,  overlap=coneH*0.30f, baseR=totalH*0.32f;

        var trunk = new GameObject("Trunk");
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localPosition = new Vector3(0f, trunkH*0.5f, 0f);
        trunk.AddComponent<MeshFilter>().sharedMesh = MakeCylinder(trunkR, trunkH, 6);
        trunk.AddComponent<MeshRenderer>().sharedMaterial = FlatMat(trunkColor);

        for (int i = 0; i < 3; i++)
        {
            float t=i/3f;
            float r=Mathf.Lerp(baseR,baseR*0.45f,t), h=Mathf.Lerp(coneH,coneH*0.75f,t);
            float yOff=trunkH*0.85f+i*(coneH-overlap);
            Color col=foliage*Mathf.Lerp(1f,1.18f,t); col.a=1f;
            var cone=new GameObject($"F{i}");
            cone.transform.SetParent(root.transform,false);
            cone.transform.localPosition=new Vector3(0f,yOff,0f);
            cone.AddComponent<MeshFilter>().sharedMesh=MakeCone(r,h,7);
            cone.AddComponent<MeshRenderer>().sharedMaterial=FlatMat(col);
        }
    }

    // =========================================================================
    // MESH BUILDERS
    // =========================================================================

    Mesh MakeCylinder(float radius, float height, int sides)
    {
        var v=new List<Vector3>(); var n=new List<Vector3>(); var t=new List<int>();
        float half=height*0.5f;
        for(int i=0;i<sides;i++){
            float a0=(float)i/sides*Mathf.PI*2f,a1=(float)(i+1)/sides*Mathf.PI*2f;
            Vector3 b0=new Vector3(Mathf.Cos(a0)*radius,-half,Mathf.Sin(a0)*radius);
            Vector3 b1=new Vector3(Mathf.Cos(a1)*radius,-half,Mathf.Sin(a1)*radius);
            Vector3 t0=new Vector3(Mathf.Cos(a0)*radius, half,Mathf.Sin(a0)*radius);
            Vector3 t1=new Vector3(Mathf.Cos(a1)*radius, half,Mathf.Sin(a1)*radius);
            Vector3 fn=Vector3.Cross(t1-b0,b1-b0).normalized;
            int b=v.Count; v.AddRange(new[]{b0,b1,t0,t1}); n.AddRange(new[]{fn,fn,fn,fn});
            t.AddRange(new[]{b,b+2,b+1,b+1,b+2,b+3});
        }
        var m=new Mesh(); m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t,0); m.RecalculateBounds(); return m;
    }

    Mesh MakeCone(float radius, float height, int sides)
    {
        var v=new List<Vector3>(); var n=new List<Vector3>(); var t=new List<int>();
        Vector3 tip=new Vector3(0f,height,0f); Vector3 capN=Vector3.down;
        for(int i=0;i<sides;i++){
            float a0=(float)i/sides*Mathf.PI*2f,a1=(float)(i+1)/sides*Mathf.PI*2f;
            Vector3 b0=new Vector3(Mathf.Cos(a0)*radius,0f,Mathf.Sin(a0)*radius);
            Vector3 b1=new Vector3(Mathf.Cos(a1)*radius,0f,Mathf.Sin(a1)*radius);
            Vector3 fn=Vector3.Cross(b1-b0,tip-b0).normalized;
            int b=v.Count; v.AddRange(new[]{b0,b1,tip}); n.AddRange(new[]{fn,fn,fn}); t.AddRange(new[]{b,b+1,b+2});
        }
        for(int i=0;i<sides;i++){
            float a0=(float)i/sides*Mathf.PI*2f,a1=(float)(i+1)/sides*Mathf.PI*2f;
            Vector3 b0=new Vector3(Mathf.Cos(a0)*radius,0f,Mathf.Sin(a0)*radius);
            Vector3 b1=new Vector3(Mathf.Cos(a1)*radius,0f,Mathf.Sin(a1)*radius);
            int b=v.Count; v.AddRange(new[]{Vector3.zero,b1,b0}); n.AddRange(new[]{capN,capN,capN}); t.AddRange(new[]{b,b+1,b+2});
        }
        var m=new Mesh(); m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t,0); m.RecalculateBounds(); return m;
    }

    Material FlatMat(Color c){var m=new Material(Shader.Find("Unlit/Color"));m.color=c;return m;}
    static int NextPow2(int v){int p=1;while(p<v)p<<=1;return p;}
}