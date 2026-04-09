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

        PaintTerrain(t[BL].terrainData, BiomeType.Grassy);
        PaintTerrain(t[BR].terrainData, BiomeType.Lake);
        PaintTerrain(t[TL].terrainData, BiomeType.Snowy);
        PaintTerrain(t[TR].terrainData, BiomeType.Rocky);

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

        try
        {
            go.tag = "Ground";
        }
        catch (UnityException)
        {
            Debug.LogWarning("Tag 'Ground' does not exist. Add it in the Tag Manager.");
        }

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

    void PaintTerrain(TerrainData td, BiomeType biome)
    {
        Texture2D texA, texB;
        Color colA, colB;
        float tileA, tileB;

        switch (biome)
        {
            case BiomeType.Lake:
                texA=sandTex;  colA=new Color(0.82f,0.74f,0.45f); tileA=6f;
                texB=grassTex; colB=new Color(0.18f,0.52f,0.15f); tileB=8f;
                break;
            case BiomeType.Snowy:
                texA=snowTex; colA=new Color(0.90f,0.93f,0.96f); tileA=8f;
                texB=dirtTex; colB=new Color(0.50f,0.33f,0.16f); tileB=6f;
                break;
            case BiomeType.Rocky:
                texA=rockTex; colA=new Color(0.48f,0.46f,0.44f); tileA=5f;
                texB=dirtTex; colB=new Color(0.50f,0.33f,0.16f); tileB=6f;
                break;
            default:
                texA=grassTex; colA=new Color(0.18f,0.52f,0.15f); tileA=8f;
                texB=dirtTex;  colB=new Color(0.50f,0.33f,0.16f); tileB=6f;
                break;
        }

        td.terrainLayers = SaveLayers(biome.ToString(), texA, colA, tileA, texB, colB, tileB);

        int res = td.alphamapResolution;
        var map = new float[res, res, 2];
        Vector2 lakeCentre = new Vector2(0.5f, 0.5f);

        for (int z = 0; z < res; z++)
        for (int x = 0; x < res; x++)
        {
            float nx    = (float)x / (res - 1);
            float nz    = (float)z / (res - 1);
            float steep = td.GetSteepness(nx, nz);
            float normH = td.GetInterpolatedHeight(nx, nz) / td.size.y;
            float wB;   // weight of layer B

            switch (biome)
            {
                case BiomeType.Grassy:
                    wB = Mathf.Clamp01((steep - 12f) / 10f);
                    break;
                case BiomeType.Lake:
                    // Sand fills a large circle, grass only at the edges
                    float dist  = Vector2.Distance(new Vector2(nx, nz), lakeCentre);
                    float sandT = 1f - Mathf.Clamp01((dist - lakeRadius) / (shoreWidth * 2f));
                    wB = 1f - sandT; // wB = grass weight
                    break;
                case BiomeType.Snowy:
                    // texA=snow, texB=dirt on slopes
                    wB = Mathf.Clamp01((steep - 10f) / 8f);
                    break;
                case BiomeType.Rocky:
                    // texA=rock, texB=dirt on steep slopes
                    wB = Mathf.Clamp01((steep - 15f) / 10f);
                    break;
                default:
                    wB = 0f;
                    break;
            }

            map[z, x, 0] = 1f - wB;
            map[z, x, 1] = wB;
        }

        td.SetAlphamaps(0, 0, map);
    }

    TerrainLayer[] SaveLayers(string id,
        Texture2D tA, Color cA, float sA,
        Texture2D tB, Color cB, float sB)
    {
        var lA = new TerrainLayer { diffuseTexture = tA ?? SolidTex(cA), tileSize = Vector2.one * sA };
        var lB = new TerrainLayer { diffuseTexture = tB ?? SolidTex(cB), tileSize = Vector2.one * sB };

#if UNITY_EDITOR
        string folder = "Assets/GeneratedTerrains";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "GeneratedTerrains");

        string pA = $"{folder}/Layer_{id}_A.terrainlayer";
        string pB = $"{folder}/Layer_{id}_B.terrainlayer";
        AssetDatabase.DeleteAsset(pA); AssetDatabase.DeleteAsset(pB);
        AssetDatabase.CreateAsset(lA, pA); AssetDatabase.CreateAsset(lB, pB);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        lA = AssetDatabase.LoadAssetAtPath<TerrainLayer>(pA);
        lB = AssetDatabase.LoadAssetAtPath<TerrainLayer>(pB);
#endif
        return new[] { lA, lB };
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