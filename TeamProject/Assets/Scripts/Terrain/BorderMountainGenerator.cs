using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns a ring of steep cone mountains around the map border.
/// No terrain system — just simple meshes with a texture slapped on.
///
/// SETUP:
/// 1. Create an empty GameObject → name it "BorderMountains"
/// 2. Attach this script
/// 3. Right-click → "Generate Border Mountains", or press Play
/// </summary>
[ExecuteInEditMode]
public class BorderMountainGenerator : MonoBehaviour
{
    [Header("Map Size — match your terrain generator")]
    public float mapWidth  = 512f;
    public float mapLength = 512f;
    public Vector3 mapOrigin = Vector3.zero;

    [Header("Mountain Shape")]
    [Tooltip("How many mountains to place around the border")]
    public int mountainCount = 40;
    [Tooltip("Min/max mountain height")]
    public Vector2 heightRange = new Vector2(60f, 120f);
    [Tooltip("Min/max mountain base radius")]
    public Vector2 radiusRange = new Vector2(30f, 60f);
    [Tooltip("Extra width multiplier — increase to make mountains fatter")]
    [Range(0.5f, 5f)] public float widthScale = 1f;
    [Tooltip("How far outside the map edge mountains are centred")]
    public float borderOffset = 20f;
    [Tooltip("Sides of each cone — low = jagged low-poly, high = smooth")]
    [Range(5, 16)] public int coneSides = 7;
    [Tooltip("How many vertical rings — more = smoother rounded top")]
    [Range(2, 12)] public int coneStacks = 6;
    [Tooltip("0 = sharp cone tip, 1 = fully domed top")]
    [Range(0f, 1f)] public float roundness = 0.6f;

    [Header("Texture")]
    public Texture2D mountainTexture;
    public Color mountainColor = new Color(0.42f, 0.40f, 0.38f);

    [Header("Randomization")]
    public int  seed                = 7;
    public bool randomizeSeedOnPlay = false;

    private GameObject    _root;
    private System.Random _rng;

    // =========================================================================

    void Start()
    {
        if (randomizeSeedOnPlay) seed = Random.Range(0, 99999);
        GenerateBorderMountains();
    }

    [ContextMenu("Generate Border Mountains")]
    public void GenerateBorderMountains()
    {
        _rng = new System.Random(seed);
        CleanUp();

        _root = new GameObject("BorderMountains_Root");
        _root.transform.SetParent(transform);

        // Distribute mountains evenly around all 4 edges
        int perEdge = mountainCount / 4;

        SpawnEdge(perEdge, EdgeAxis.South);
        SpawnEdge(perEdge, EdgeAxis.North);
        SpawnEdge(perEdge, EdgeAxis.West);
        SpawnEdge(perEdge, EdgeAxis.East);

        Debug.Log($"[BorderMountains] Spawned {mountainCount} mountains.");
    }

    void CleanUp()
    {
        var old = GameObject.Find("BorderMountains_Root");
        if (old != null) DestroyImmediate(old);
        if (_root != null) DestroyImmediate(_root);
    }

    enum EdgeAxis { South, North, West, East }

    void SpawnEdge(int count, EdgeAxis edge)
    {
        for (int i = 0; i < count; i++)
        {
            // Spread mountains evenly along the edge with small random jitter
            float t     = (float)i / count + ((float)_rng.NextDouble() - 0.5f) * (1f / count);
            float jitter = borderOffset * (0.5f + (float)_rng.NextDouble() * 0.8f);

            Vector3 pos = edge switch
            {
                EdgeAxis.South => new Vector3(mapOrigin.x + t * mapWidth,  mapOrigin.y, mapOrigin.z - jitter),
                EdgeAxis.North => new Vector3(mapOrigin.x + t * mapWidth,  mapOrigin.y, mapOrigin.z + mapLength + jitter),
                EdgeAxis.West  => new Vector3(mapOrigin.x - jitter,        mapOrigin.y, mapOrigin.z + t * mapLength),
                EdgeAxis.East  => new Vector3(mapOrigin.x + mapWidth + jitter, mapOrigin.y, mapOrigin.z + t * mapLength),
                _              => Vector3.zero
            };

            float h = heightRange.x + (float)_rng.NextDouble() * (heightRange.y - heightRange.x);
            float r = radiusRange.x + (float)_rng.NextDouble() * (radiusRange.y - radiusRange.x);
            float yRot = (float)_rng.NextDouble() * 360f;

            SpawnMountain(pos, h, r * widthScale, yRot);
        }
    }

    void SpawnMountain(Vector3 basePos, float height, float radius, float yRot)
    {
        var go = new GameObject("Mountain");
        go.transform.SetParent(_root.transform);
        go.transform.position = basePos;
        go.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

        // Main cone
        var mf  = go.AddComponent<MeshFilter>();
        var mr  = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = BuildCone(radius, height, coneSides);
        mr.sharedMaterial = MakeMaterial();

        // Add a MeshCollider so the player actually collides with it
        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = mf.sharedMesh;

        // Zero friction + full bounciness = player slides straight off
        var slippery = new PhysicsMaterial("MountainSlippery");
        slippery.dynamicFriction = 0f;
        slippery.staticFriction  = 0f;
        slippery.bounciness      = 0f;
        slippery.frictionCombine = PhysicsMaterialCombine.Minimum;
        slippery.bounceCombine   = PhysicsMaterialCombine.Minimum;
        mc.sharedMaterial = slippery;
    }

    // ── Mesh ──────────────────────────────────────────────────────────────────

    Mesh BuildCone(float radius, float height, int sides)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs   = new List<Vector2>();
        var tris  = new List<int>();

        // Build stacked rings from base to tip.
        // The radius at each stack level follows a rounded profile:
        //   - near the base: full radius (linear)
        //   - near the top:  curves inward faster (dome shape)
        // We blend between a straight cone (t) and a sine curve based on roundness.

        int stacks = coneStacks;

        // Generate ring vertices
        // rings[s][i] = vertex index of ring s, point i
        int[][] rings = new int[stacks + 1][];

        for (int s = 0; s <= stacks; s++)
        {
            float tLinear = 1f - (float)s / stacks;           // 1 at base, 0 at top
            // Sine curve makes radius shrink faster near the top → rounded look
            float tRound  = Mathf.Sin(tLinear * Mathf.PI * 0.5f);
            float tFinal  = Mathf.Lerp(tLinear, tRound, roundness);

            float ringRadius = radius * tFinal;
            float ringHeight = height * (float)s / stacks;

            rings[s] = new int[sides];
            for (int i = 0; i < sides; i++)
            {
                float angle = (float)i / sides * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * ringRadius;
                float z = Mathf.Sin(angle) * ringRadius;

                rings[s][i] = verts.Count;
                verts.Add(new Vector3(x, ringHeight, z));
                // Approximate normal: blend outward direction with upward tilt
                Vector3 outDir = new Vector3(x, 0f, z).normalized;
                Vector3 upDir  = Vector3.up;
                norms.Add(Vector3.Lerp(outDir, upDir, (float)s / stacks).normalized);
                uvs.Add(new Vector2((float)i / sides, (float)s / stacks));
            }
        }

        // Tip vertex
        int tipIdx = verts.Count;
        verts.Add(new Vector3(0f, height, 0f));
        norms.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 1f));

        // Stitch quads between rings — add both windings for double-sided
        for (int s = 0; s < stacks; s++)
        {
            for (int i = 0; i < sides; i++)
            {
                int i1 = (i + 1) % sides;
                int a = rings[s][i],   b = rings[s][i1];
                int c = rings[s+1][i], d = rings[s+1][i1];

                // Front face
                tris.AddRange(new[] { a, b, c });
                tris.AddRange(new[] { b, d, c });
                // Back face (reversed)
                tris.AddRange(new[] { a, c, b });
                tris.AddRange(new[] { b, c, d });
            }
        }

        // Top cap — connect top ring to tip
        int topRing = stacks; // index of the topmost ring (smallest radius)
        // We skip the top ring stitching if radius is effectively 0 — just connect directly
        for (int i = 0; i < sides; i++)
        {
            int i1 = (i + 1) % sides;
            int a = rings[topRing][i], b = rings[topRing][i1];
            tris.AddRange(new[] { a, b, tipIdx });
            tris.AddRange(new[] { a, tipIdx, b }); // back face
        }

        // Bottom cap
        int botCentre = verts.Count;
        verts.Add(Vector3.zero);
        norms.Add(Vector3.down);
        uvs.Add(new Vector2(0.5f, 0f));

        for (int i = 0; i < sides; i++)
        {
            int i1 = (i + 1) % sides;
            tris.AddRange(new[] { botCentre, rings[0][i1], rings[0][i] });
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── Material ──────────────────────────────────────────────────────────────

    Material MakeMaterial()
    {
        Material mat;
        if (mountainTexture != null)
        {
            mat = new Material(Shader.Find("Unlit/Texture") ?? Shader.Find("Unlit/Color"));
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", mountainTexture);
        }
        else
        {
            mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = mountainColor;
        }
        return mat;
    }
}