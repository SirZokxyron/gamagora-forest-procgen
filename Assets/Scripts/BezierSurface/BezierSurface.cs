using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;

using Random = UnityEngine.Random;

public class BezierSurface : MonoBehaviour
{
    [Header("Surface Param")]
    [SerializeField, Range(0, 1000000)] int seed = 0;
    [SerializeField, Range(1, 200)] float planeSize = 5f;
    [SerializeField, Range(1, 20)] float zoomPerlinArea = 5f;
    [SerializeField, Range(0, 10)] int subdivisionDepth = 0;
    [SerializeField, Range(0f, 10f)] float noiseAmp = 1f;
    [SerializeField] private Material groundMaterial;

    [Header("Surface Debug")]
    [SerializeField] int sideCount;
    Vector3[,] rawNodes;
    Vector3[,] C2Nodes;

    [Header("Bezier Debug")]
    [SerializeField, Range(2, 100)] int bezierLinspace = 10;
    [SerializeField] Func<float, float>[] BernsteinPoly = new Func<float, float>[4];
    [SerializeField] float[] time;
    [SerializeField] Vector3[,] bezierNodes;
    [SerializeField] bool defaultCurve = true;
    [SerializeField] bool smoothCurve = true;

    public void SetSeed(int seed, float planeSize)
    {
        this.seed = seed;
        this.planeSize = planeSize;

        sideCount = 4 + subdivisionDepth * 3;
        rawNodes = new Vector3[sideCount, sideCount];
        C2Nodes = new Vector3[sideCount, sideCount];

        GeneratePerlinSurface();

        C2PreProcessing();

        for (int i = 0; i < 4; ++i)
            BernsteinPoly[i] = Bernstein(3, i);

        GenerateBezierSurface();
        CreateMeshSurface();
    }

    void Start() {
        sideCount = 4 + subdivisionDepth * 3;
        rawNodes = new Vector3[sideCount, sideCount];
        C2Nodes = new Vector3[sideCount, sideCount];

        GeneratePerlinSurface();

        C2PreProcessing();

        for (int i = 0; i < 4; ++i) 
            BernsteinPoly[i] = Bernstein(3, i);

        GenerateBezierSurface();
        CreateMeshSurface();
    }

    // For debug purposes
    void OnDrawGizmos() {
        if(defaultCurve || smoothCurve)
        {
            sideCount = 4 + subdivisionDepth * 3;
            rawNodes = new Vector3[sideCount, sideCount];
            C2Nodes = new Vector3[sideCount, sideCount];

            GeneratePerlinSurface();
        }

        if(defaultCurve)
        {
            Gizmos.color = Color.white;
            for (int xi = 0; xi < sideCount - 1; ++xi)
                for (int zi = 0; zi < sideCount - 1; ++zi)
                {
                    Gizmos.DrawLine(rawNodes[xi, zi], rawNodes[xi + 1, zi]);
                    Gizmos.DrawLine(rawNodes[xi, zi], rawNodes[xi, zi + 1]);
                }

            for (int i = 0; i < sideCount - 1; ++i)
            {
                Gizmos.DrawLine(rawNodes[i, sideCount - 1], rawNodes[i + 1, sideCount - 1]);
                Gizmos.DrawLine(rawNodes[sideCount - 1, i], rawNodes[sideCount - 1, i + 1]);
            }
        }
        


        if(smoothCurve)
        {
            C2PreProcessing();
            for (int i = 0; i < 4; ++i)
                BernsteinPoly[i] = Bernstein(3, i);
            GenerateBezierSurface();

            int bezierSideCount = (subdivisionDepth + 1) * time.Length;

            Gizmos.color = Color.red;
            for (int xi = 0; xi < bezierSideCount - 1; ++xi)
                for (int zi = 0; zi < bezierSideCount - 1; ++zi)
                {
                    Gizmos.DrawLine(bezierNodes[xi, zi], bezierNodes[xi + 1, zi]);
                    Gizmos.DrawLine(bezierNodes[xi, zi], bezierNodes[xi, zi + 1]);
                }

            for (int i = 0; i < bezierSideCount - 1; ++i)
            {
                Gizmos.DrawLine(bezierNodes[i, bezierSideCount - 1], bezierNodes[i + 1, bezierSideCount - 1]);
                Gizmos.DrawLine(bezierNodes[bezierSideCount - 1, i], bezierNodes[bezierSideCount - 1, i + 1]);
            }

        }
    }

    // ======================= //
    // === Perlin Surface  === //
    // ======================= //

    void GeneratePerlinSurface() {
        Random.InitState(seed);

        float[] t = Linspace(0, planeSize, sideCount);

        float rngDeltaX = Random.Range(0f, 1000000f);
        float rngDeltaZ = Random.Range(0f, 1000000f);

        float yi;
        for (int xi = 0; xi < sideCount; ++xi) 
        for (int zi = 0; zi < sideCount; ++zi) {
            yi = Mathf.PerlinNoise(t[xi]/zoomPerlinArea + rngDeltaX, t[zi]/zoomPerlinArea + rngDeltaZ) * noiseAmp;
            rawNodes[xi, zi] = new Vector3(t[xi], yi, t[zi]);
        }
    }

    // ======================== //
    // === Helper Functions === //
    // ======================== //

    // Returns a vector a float values evenly spaced between `a` and `b`
    float[] Linspace(float a, float b, int amount)
    {
        float delta = (b - a) / (amount - 1);
        return Enumerable.Range(0, amount).Select(x => a + x * delta).ToArray();
    }

    // Cache-optimized factorial
    Dictionary<int, int> factorialCache = new Dictionary<int, int>();
    int Factorial(int n) {
        if (factorialCache.ContainsKey(n))
            return factorialCache[n];

        if (n == 0) {
            factorialCache[0] = 1;
            return 1;
        }

        factorialCache.Add(n, n * Factorial(n-1));
        return factorialCache[n];
    }

    // ==================== //
    // === Cubic Bezier === //
    // ==================== //

    void C2PreProcessing() {
        // Copy raw nodes into smoothed C2 nodes
        for (int xi = 0; xi < sideCount; ++xi) 
        for (int zi = 0; zi < sideCount; ++zi) {
            C2Nodes[xi, zi] = rawNodes[xi, zi];
        }

        Vector3 C1, C2, newPivot;

        // X pass
        for (int xi = 0; xi < sideCount; ++xi) 
        for (int zi = 0; zi < subdivisionDepth; ++zi) {
            C1 = rawNodes[xi, 2 + zi * 3];
            // pivot = rawNodes[xi, 3 + zi * 3]; //? Was used before
            C2 = rawNodes[xi, 4 + zi * 3]; 

            // After preprocessing
            newPivot = (C1 + C2) / 2;
            C2Nodes[xi, 3 + zi * 3] = newPivot;
        }

        // Z pass
        for (int xi = 0; xi < subdivisionDepth; ++xi) 
        for (int zi = 0; zi < sideCount; ++zi) {
            C1 = C2Nodes[2 + xi * 3, zi];
            // pivot = C2Nodes[3 + xi * 3, zi]; //? Was used before
            C2 = C2Nodes[4 + xi * 3, zi];

            // After preprocessing
            newPivot = (C1 + C2) / 2;
            C2Nodes[3 + xi * 3, zi] = newPivot;

        }

        //! C2 continuity for surface is hard.
        // Corner pass
        // for (int xi = 0; xi < subdivisionDepth; ++xi) 
        // for (int zi = 0; zi < subdivisionDepth; ++zi) {
        //     C1 = C2Nodes[2 + xi * 3, 2 + zi * 3];
        //     pivot = C2Nodes[3 + xi * 3, 3 + zi * 3];
        //     C2 = C2Nodes[4 + xi * 3, 4 + zi * 3];

        //     // After preprocessing
        //     newPivot = (C1 + C2) / 2;
        //     C2Nodes[3 + xi * 3, 3 + zi * 3] = newPivot;

        // }
    }

    Func<float, float> Bernstein(int degree, int i)
    {
        return t => Factorial(degree) / (Factorial(i) * Factorial(degree-i)) * Mathf.Pow(t, i) * Mathf.Pow(1 - t, degree - i);
    }

    Func<float, float, Vector3> Bezier2D(int x, int z)
    {
        Func<float, float, Vector3> P = (u, v) => {
            Vector3 res = Vector3.zero;
            for (int i = 0; i < 4; ++i) {
                for (int j = 0; j < 4; ++j)
                {
                    res += BernsteinPoly[i](u) * BernsteinPoly[j](v) * C2Nodes[3*x+i, 3*z+j];
                }
            }
            return res;
        };
        return P;
    }

    void GenerateBezierSurface() {
        time = Linspace(0, 1, bezierLinspace);

        int bezierSideCount = (subdivisionDepth + 1) * time.Length;
        bezierNodes = new Vector3[bezierSideCount, bezierSideCount];

        Func<float, float, Vector3> P;
        for (int xi = 0; xi < subdivisionDepth+1; ++xi)
        for (int zi = 0; zi < subdivisionDepth+1; ++zi) {
            P = Bezier2D(xi, zi);
            for (int ui = 0; ui < time.Length; ++ui)
            for (int vi = 0; vi < time.Length; ++vi) {
                bezierNodes[xi * time.Length + ui, zi * time.Length + vi] = P(time[ui], time[vi]); 
            }
        }
    }

    // ============ //
    // === Mesh === //
    // ============ //

    void CreateMeshSurface() {
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();

        int side = (subdivisionDepth + 1) * time.Length;
        int verticesN = side * side;

        Vector3[] vertices = new Vector3[verticesN];
        List<int> triangles = new List<int>();
        Vector2[] uv = new Vector2[vertices.Length];

        int id = 0;
        for (int zi = 0; zi < (subdivisionDepth + 1) * time.Length; ++zi)
        for (int xi = 0; xi < (subdivisionDepth + 1) * time.Length; ++xi) {
            vertices[id] = bezierNodes[xi, zi];
            id += 1;
        }

        for (int i = 0; i < verticesN-side-1; ++i) {
            if ((i+1) % side == 0) continue;
            // First half
            triangles.Add(i);
            triangles.Add(i + side + 1);
            triangles.Add(i + 1);

            // Second half
            triangles.Add(i);
            triangles.Add(i + side);
            triangles.Add(i + side + 1);
        }

        var uvs = new Vector2[vertices.Length];

        for (int u = 0; u < uvs.Length; u++)
            uvs[u] = new Vector2(vertices[u].x * transform.localScale.x, vertices[u].y * transform.localScale.y);


        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        gameObject.GetComponent<MeshFilter>().mesh = mesh;
        gameObject.GetComponent<MeshRenderer>().material = groundMaterial;
    }

    // ================= //
    // === Interface === //
    // ================= //

    // Randomise seed for the perlin noise rng
    void RandomiseSeed() {
        seed = Random.Range(0, 10000000);
    }

    // Returns a point on the bezier surface considering it as a big parametric surface, xt and zt are therefore in [0, 1]
    public Vector3 GetBezierAt(float xt, float zt) {
        float delta = 1 / ((float)subdivisionDepth + 1);
        int xi;
        int zi;
        for (xi = 0; (xi+1) * delta <= xt; ++xi);
        for (zi = 0; (zi+1) * delta <= zt; ++zi);
        
        if (xt == 1) --xi;
        if (zt == 1) --zi;

        float relativeX = (xt - xi*delta) / delta;
        float relativeZ = (zt - zi*delta) / delta;

        // Debug.Log($"xt:{xt}, zt:{zt}, xi:{xi}, zi:{zi}, relX:{relativeX}, relZ:{relativeZ}");

        return Bezier2D(xi, zi)(relativeX, relativeZ);
    }
}
