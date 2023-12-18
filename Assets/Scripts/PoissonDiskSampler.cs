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

    /// <summary>
    /// Create a Poisson Disk Sampler with parameters.
    /// </summary>
    /// <param name="width">Width of ground</param>
    /// <param name="height">Height of ground</param>
    /// <param name="seed">Random seed</param>
    /// <param name="minDist">Minimum distance between points</param>
    /// <param name="newPointsCount">Points to test and create/remove from an other point</param>
    /// <param name="perlinScale">Size of the noise</param>
    /// <param name="minNoise">Minimum coefficient for the minimum distance from the noise</param>
    /// <param name="maxNoise">Maximum coefficient for the maximum distance from the noise</param>
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

    /// <summary>
    /// Create a dictionnary of minimum distance calculated from a default minimum distance
    /// </summary>
    /// <param name="size">Size of the dictionnary</param>
    /// <param name="minDist">Default minimum distance</param>
    /// <returns>dictionnary of minimum distance by position</returns>
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


    /// <summary>
    /// Random number from a maximum
    /// </summary>
    /// <param name="max">Maximum</param>
    /// <returns>integer between 0 and maximum exclude</returns>
    int Rand(int max)
    {
        return Random.Range(0, max);
    }

    /// <summary>
    /// Coroutine to generate a sample from parameters.
    /// </br>
    /// The coroutine prevent unity from freezing and allow the generation to work gradually
    /// </summary>
    /// <returns>List of points</returns>
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


    /// <summary>
    /// Check if a point is in our bounds
    /// </summary>
    /// <param name="point">Point to check</param>
    /// <param name="width">Width of the ground</param>
    /// <param name="height">Height (or depth) of the ground</param>
    /// <returns>boolean</returns>
    private bool InRectangle(Vector2 point, int width, int height)
    {
        return point.x >= 0 && point.x < width && point.y >= 0 && point.y < height;
    }

    /// <summary>
    /// Return the point in the grid where the point parameter is
    /// </summary>
    /// <param name="point">Point in world position</param>
    /// <param name="cellSize">Size of the cells in the grid</param>
    /// <returns>Vector2Int representing a cell</returns>
    private Vector2Int ImageToGrid(Vector2 point, float cellSize)
    {
        int gridX = (int)(point.x / cellSize);
        int gridY = (int)(point.y / cellSize);
        return new Vector2Int(gridX, gridY);
    }

    /// <summary>
    /// Generate a random point in a circle around the point parameter.
    /// </br>
    /// The point is in a ring around the point where the minimum distance is minDistance and
    /// the maximum distance is 2*minDistance.
    /// </summary>
    /// <param name="point"></param>
    /// <param name="currentMinDist"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Check if a point is near another. It checks all point in a distance of 5 grid cells.
    /// </summary>
    /// <param name="grid">Dictionnary representing sampled point, points are Vector2? because some cells can be empty</param>
    /// <param name="point">Point to check</param>
    /// <param name="currentMinDist">Minimum distance at point position</param>
    /// <param name="cellSize">Size of the cell</param>
    /// <returns>boolean</returns>
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

    /// <summary>
    /// Returns all cells in a grid from a point
    /// </summary>
    /// <param name="grid">Dictionnary of point</param>
    /// <param name="gridPoint">Point in the grid</param>
    /// <param name="radius">Cells around the point to check</param>
    /// <returns>List of all points that can be null around a point</returns>
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
