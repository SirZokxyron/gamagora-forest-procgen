using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PoissonDiskSampler
{
    private readonly float perlinOffset = 0;

    private readonly float perlinScale = 1f;
    private readonly float minNoise = 1f;
    private readonly float maxNoise = 2f;

    private readonly int width;
    private readonly int height;
    private readonly float minDist;
    private readonly int newPointsCount;

    public PoissonDiskSampler(int width, int height, 
        int seed = 771, float minDist = 10, int newPointsCount = 1, 
        float perlinScale = 1f, float minNoise = 1f, float maxNoise = 2f)
    {
        this.width = width;
        this.height = height;
        this.minDist = minDist;
        this.newPointsCount = newPointsCount;

        this.perlinScale = perlinScale;
        this.minNoise = minNoise;
        this.maxNoise = maxNoise;

        Random.InitState(seed);

        perlinOffset = Random.Range(0, 100000);
    }

    public PoissonDiskSampler(float width, float height,
        int seed = 771, float minDist = 10, int newPointsCount = 1,
        float perlinScale = 1f, float minNoise = 1f, float maxNoise = 2f) 
        : this((int) width, (int) height, seed, minDist, newPointsCount, perlinScale, minNoise, maxNoise)
    {}

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

    int Rand(int max)
    {
        return Random.Range(0, max);
    }

    public IEnumerator GeneratePoisson()
    {
        float currentMinDist = minDist;

        float cellSize = currentMinDist / Mathf.Sqrt(2);

        Dictionary<Vector2Int, Vector2?> grid = new Dictionary<Vector2Int, Vector2?>();
        for (int i = 0; i < Mathf.Ceil(width / cellSize); i++)
        {
            for (int j = 0; j < Mathf.Ceil(height / cellSize); j++)
            {
                grid.Add(new Vector2Int(i, j), null);
            }
        }

        Dictionary<Vector2Int, float> grey
            = Grey(new Vector2(Mathf.Ceil(width / cellSize), Mathf.Ceil(height / cellSize)), currentMinDist);

        RandomQueue processList = new RandomQueue();
        List<Vector2> samplePoints = new List<Vector2>();
        Vector2 firstPoint = new Vector2(Rand(width), Rand(height));

        processList.push(firstPoint);
        samplePoints.Add(firstPoint);
        grid[ImageToGrid(firstPoint, cellSize)] = firstPoint;
        while (!processList.empty())
        {
            Vector2 point = processList.pop();
            Vector2 gridPoint = ImageToGrid(point, cellSize);
            currentMinDist = grey[Vector2Int.FloorToInt(gridPoint)];
            for (int i = 0; i < newPointsCount; i++)
            {
                Vector2 newPoint = GenerateRandomPointAround(point, currentMinDist);

                if (InRectangle(newPoint, width, height) && !InNeighbourhood(grid, newPoint, currentMinDist, cellSize))
                {
                    processList.push(newPoint);
                    samplePoints.Add(newPoint);
                    grid[ImageToGrid(newPoint, cellSize)] = newPoint;
                }
            }
            yield return samplePoints;
            samplePoints = new List<Vector2>();
        }

        yield return "success";
    }


    private bool InRectangle(Vector2 point, int width, int height)
    {
        return point.x >= 0 && point.x < width && point.y >= 0 && point.y < height;
    }

    private Vector2Int ImageToGrid(Vector2 point, float cellSize)
    {
        int gridX = (int)(point.x / cellSize);
        int gridY = (int)(point.y / cellSize);
        return new Vector2Int(gridX, gridY);
    }

    private Vector2 GenerateRandomPointAround(Vector2 point, float currentMinDist)
    {
        float r1 = Random.value;
        float r2 = Random.value;

        // max dist = 2 * min dist
        float radius = currentMinDist * (r1 + 1);
        float angle = 2 * Mathf.PI * r2;
        float newX = point.x + radius * Mathf.Cos(angle);
        float newY = point.y + radius * Mathf.Sin(angle);
        return new Vector2(newX, newY);
    }

    private bool InNeighbourhood(Dictionary<Vector2Int, Vector2?> grid, Vector2 point, float currentMinDist, float cellSize)
    {
        Vector2 gridPoint = ImageToGrid(point, cellSize);

        List<Vector2?> cellsAroundPoint = SquareAroundPoint(grid, gridPoint, 5);
        foreach (Vector2? cell in cellsAroundPoint)
        {
            if (cell is Vector2 cellV)
            {
                if (Vector2.Distance(cellV, point) < currentMinDist)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private List<Vector2?> SquareAroundPoint(Dictionary<Vector2Int, Vector2?> grid, Vector2 gridPoint, float radius)
    {
        List<Vector2?> pts = new List<Vector2?>();

        int minIndexX = (int)(gridPoint.x - radius);
        int maxIndexX = (int)(gridPoint.x + radius);
        int minIndexY = (int)(gridPoint.y - radius);
        int maxIndexY = (int)(gridPoint.y + radius);

        for (int i = minIndexX; i < maxIndexX; i++)
            for (int j = minIndexY; j < maxIndexY; j++)
                if (grid.TryGetValue(new Vector2Int(i, j), out Vector2? value))
                    pts.Add(value);

        return pts;
    }
}
