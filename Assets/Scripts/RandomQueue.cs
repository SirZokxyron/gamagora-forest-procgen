using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomQueue
{
    List<Vector2> points;

    public bool empty()
    {
        return points.Count == 0;
    }

    public void push(Vector2 value)
    {
        points.Add(value);
    }

    public Vector2 pop()
    {
        int randomIndex = Random.Range(0, points.Count - 1);
        Vector2 pt = points[randomIndex];
        points.RemoveAt(randomIndex);
        return pt;
    }

    public RandomQueue()
    {
        points = new List<Vector2>();
    }
}
