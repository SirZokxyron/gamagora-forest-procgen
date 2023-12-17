using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateForest : MonoBehaviour
{
    [Header("Terrain Parameters")]
    [SerializeField, Range(10, 500)] private float width = 20;
    [SerializeField, Range(10, 500)] private float height = 20;
    [SerializeField, Range(1, 10000000)] private int seed = 771;

    [Header("Poisson Disk Parameters")]
    [SerializeField, Range(1, 20)] private float defaultDistance;
    [SerializeField, Range(0.5f, 50)] private float minNoiseCoefficient = 1f;
    [SerializeField, Range(0.5f, 50)] private float maxNoiseCoefficient = 2f;
    [SerializeField, Range(1, 20)] private int newPoints;
    [SerializeField, Range(0.1f, 6f)] private float perlinScale = 1f;

    [Header("Bezier Surface")]
    [SerializeField] private BezierSurface bezierSurface;

    [Header("L System")]
    [SerializeField] private GameObject lTree;

    PoissonDiskSampler diskSampler;
    CoroutineWithData treePlacementCoroutine;
    List<Vector2> treePositions;

    // Start is called before the first frame update
    void Start()
    {
        bezierSurface.SetSeed(seed);
        diskSampler = new PoissonDiskSampler(
            width, height, 
            seed, defaultDistance, 
            newPoints, perlinScale, 
            minNoiseCoefficient, maxNoiseCoefficient
        );
        treePlacementCoroutine = new CoroutineWithData(this, diskSampler.GeneratePoisson());
    }

    // Update is called once per frame
    void Update()
    {
        if (treePlacementCoroutine == null) return;
        if (treePlacementCoroutine.result is List<Vector2> pts)
        {
            foreach (Vector2 pt in pts)
            {
                Vector3 pos = bezierSurface.GetBezierAt(pt.x / width, pt.y / height);
                pos.x = pt.x;
                pos.z = pt.y;
                Instantiate(lTree, pos, Quaternion.identity);
            }
        } else if ((treePlacementCoroutine.result is string s) && s == "success")
        {
            Debug.Log("Over");
            treePlacementCoroutine.Stop();
        }
    }
}
