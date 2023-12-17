using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoissonTest : MonoBehaviour
{
    [Range(5, 20)]
    [SerializeField] private int bounds;
    [Range(10, 200)]
    [SerializeField] private float _minDist;
    [Range(1, 2000)]
    [SerializeField] private int seed = 10;
    [Range(1, 20)]
    [SerializeField] private int newPoints;
    [Range(0.5f, 50)]
    [SerializeField] private float minNoise = 1f;
    [Range(0.5f, 50)]
    [SerializeField] private float maxNoise = 2f;
    [Range(0.1f, 6f)]
    [SerializeField] private float perlinScale = 1f;
    [SerializeField] private RectTransform myRect;
    [SerializeField] private RawImage myImage;

    private float perlinOffset = 0;



    int Rand(int max)
    {
        return Random.Range(0, max);
    }

    Vector2Int imageToGrid(Vector2 point, float cellSize)
    {
        int gridX = (int) (point.x / cellSize);
        int gridY = (int) (point.y / cellSize);
        return new Vector2Int(gridX, gridY);
    }

    Dictionary<Vector2Int, float> Grey(Vector2 size, float minDist)
    {
        Dictionary<Vector2Int, float> grey = new Dictionary<Vector2Int, float>();
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                float xCoord = (x / size.x) * perlinScale;
                float yCoord = (y / size.y) * perlinScale; 

                float noise = Mathf.PerlinNoise(perlinOffset + xCoord, perlinOffset + yCoord);
                noise *= (maxNoise - minNoise);
                noise += minNoise;

                grey.Add(new Vector2Int(x, y), noise * minDist);
            }
        }
        return grey;
    }


    List<Vector2> generatePoisson(int width, int height, float minDist, int newPointsCount)
    {
        float cellSize = minDist / Mathf.Sqrt(2);

        Dictionary<Vector2Int, Vector2?> grid = new Dictionary<Vector2Int, Vector2?>();
        for (int i = 0; i < Mathf.Ceil(width / cellSize); i++)
        {
            for (int j = 0; j < Mathf.Ceil(height / cellSize); j++)
            {
                grid.Add(new Vector2Int(i, j), null);
            }
        }

        Dictionary<Vector2Int, float> grey 
            = Grey(new Vector2(Mathf.Ceil(width / cellSize), Mathf.Ceil(height / cellSize)), minDist);

        RandomQueue processList = new RandomQueue();
        List<Vector2> samplePoints = new List<Vector2>();

        Vector2 firstPoint = new Vector2(Rand(width), Rand(height));

        processList.push(firstPoint);
        samplePoints.Add(firstPoint);
        grid[imageToGrid(firstPoint, cellSize)] = firstPoint;
        while(!processList.empty())
        {
            Vector2 point = processList.pop();
            Vector2 gridPoint = imageToGrid(point, cellSize);
            minDist = grey[Vector2Int.FloorToInt(gridPoint)];
            for (int i = 0; i < newPointsCount; i++)
            {
                Vector2 newPoint = generateRandomPointAround(point, minDist);

                if(inRectangle(newPoint, width, height) && !inNeighbourhood(grid, newPoint, minDist, cellSize))
                {
                    processList.push(newPoint);
                    samplePoints.Add(newPoint);
                    grid[imageToGrid(newPoint, cellSize)] = newPoint;
                }
            }
        }

        return samplePoints;
    }

    bool inRectangle(Vector2 point, int width, int height)
    {
        return point.x >= 0 && point.x < width && point.y >= 0 && point.y < height;
    }

    Vector2 generateRandomPointAround(Vector2 point, float mindist)
    {
        float r1 = Random.value;
        float r2 = Random.value;

        // max dist = 2 * min dist
        float radius = mindist * (r1 + 1);
        float angle = 2 * Mathf.PI * r2;
        float newX = point.x + radius * Mathf.Cos(angle);
        float newY = point.y + radius * Mathf.Sin(angle);
        return new Vector2(newX, newY);
    }

    bool inNeighbourhood(Dictionary<Vector2Int, Vector2?> grid, Vector2 point, float minDist, float cellSize)
    {
        Vector2 gridPoint = imageToGrid(point, cellSize);

        List<Vector2?> cellsAroundPoint = squareAroundPoint(grid, gridPoint, 5);
        foreach (Vector2? cell in cellsAroundPoint)
        {
            if(cell is Vector2 cellV)
            {
                if(Vector2.Distance(cellV, point) < minDist)
                {
                    return true;
                }
            }
        }
        return false;
    }

    List<Vector2?> squareAroundPoint(Dictionary<Vector2Int, Vector2?> grid, Vector2 gridPoint, float radius)
    {
        List<Vector2?> pts = new List<Vector2?>();

        int minIndexX = (int)(gridPoint.x - radius);
        int maxIndexX = (int)(gridPoint.x + radius);
        int minIndexY = (int)(gridPoint.y - radius);
        int maxIndexY = (int)(gridPoint.y + radius);

        for (int i = minIndexX; i < maxIndexX; i++)
            for (int j = minIndexY; j < maxIndexY; j++)
                if(grid.TryGetValue(new Vector2Int(i, j), out Vector2? value))
                    pts.Add(value);

        return pts;
    }


    public Texture2D DrawCircle(Texture2D tex, Color color, int x, int y, int radius = 3)
    {
        float rSquared = radius * radius;

        for (int u = x - radius; u < x + radius + 1; u++)
            for (int v = y - radius; v < y + radius + 1; v++)
                if ((x - u) * (x - u) + (y - v) * (y - v) < rSquared)
                {
                    if (u < 0 || v < 0 || u > tex.width || v > tex.height) continue;
                    tex.SetPixel(u, v, color);
                }

        return tex;
    }

    public void DrawLine(Texture2D tex, Vector2 p1, Vector2 p2, Color col)
    {
        Vector2 t = p1;
        float frac = 1 / Mathf.Sqrt(Mathf.Pow(p2.x - p1.x, 2) + Mathf.Pow(p2.y - p1.y, 2));
        float ctr = 0;

        while ((int)t.x != (int)p2.x || (int)t.y != (int)p2.y)
        {
            t = Vector2.Lerp(p1, p2, ctr);
            ctr += frac;
            tex.SetPixel((int)t.x, (int)t.y, col);
        }
    }

    Texture2D tex;
    private void Start()
    {
        tex = new Texture2D((int)myRect.rect.width, (int)myRect.rect.height);
        myImage.texture = tex;
        GeneratePoints();
    }

    private void GeneratePoints()
    {
        //Random.InitState(seed);

        //perlinOffset = Random.Range(0, 100000);

        //List<Vector2> myPts = generatePoisson(
        //    (int)myRect.rect.width, 
        //    (int)myRect.rect.height, _minDist, newPoints);

        //List<Vector2> myPts = new PoissonDiskSampler(
        //    (int)myRect.rect.width, 
        //    (int)myRect.rect.height, 
        //    seed, _minDist, newPoints, perlinScale, minNoise, maxNoise
        //    ).GeneratePoisson();
        
        //var colorData = tex.GetPixels32();
        //for (int i = 0; i < colorData.Length; i++)
        //{
        //    colorData[i] = new Color(0, 0, 0, 0);
        //}
        //tex.SetPixels32(colorData);

        //foreach (Vector2 pt in myPts)
        //{
        //    DrawCircle(tex, Color.red, (int)pt.x, (int)pt.y, 4);
        //}

        //float cellSize = _minDist / Mathf.Sqrt(2);
        //float width = myRect.rect.width;
        //float height = myRect.rect.height;

        //for (float x = 0; x < (float)myRect.rect.width; x += cellSize)
        //{
        //    DrawLine(tex,
        //        new Vector2(x, 0),
        //        new Vector2(x, height),
        //        Color.blue
        //        );
        //    DrawLine(tex,
        //        new Vector2(0, x),
        //        new Vector2(width, x),
        //        Color.blue
        //        );
        //}

        tex.Apply();
    }


    //private void OnDrawGizmosSelected()
    //{
    //    return;
    //    bounds = 1000;
    //    _minDist = 100;
    //    newPoints = 1;
    //    Random.InitState(seed);
    //    List<Vector2> myPts = generatePoisson(bounds, bounds, _minDist, newPoints);
    //    Gizmos.color = Color.red;

    //    Gizmos.DrawLine(
    //        new Vector3(0, 0, bounds),
    //        new Vector3(bounds, 0, bounds)
    //    );

    //    Gizmos.DrawLine(
    //        new Vector3(bounds, 0, 0),
    //        new Vector3(bounds, 0, bounds)
    //    );

    //    Gizmos.DrawLine(
    //        new Vector3(0, 0, 0),
    //        new Vector3(0, 0, bounds)
    //    );

    //    Gizmos.DrawLine(
    //        new Vector3(0, 0, 0),
    //        new Vector3(bounds, 0, 0)
    //    );

    //    foreach (Vector2 pt in myPts)
    //    {
    //        Gizmos.DrawSphere(new Vector3(pt.x, pt.y, 0), 5f);
    //    }
    //}
}
