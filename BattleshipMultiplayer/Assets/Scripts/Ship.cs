using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{
    public int numCells;

    [HideInInspector]
    public bool horizontal = true;

    public Vector2[] GetCoords(Cell origin)
    {
        Vector2[] coords = new Vector2[numCells];
        if (horizontal)
        {
            for (int i = 0; i < numCells; i++)
            {
                coords[i] = new Vector2(origin.Coords.x, origin.Coords.y + i);
            }
        }
        else
        {
            for (int i = 0; i < numCells; i++)
            {
                coords[i] = new Vector2(origin.Coords.x + i, origin.Coords.y);
            }
        }
        return coords;
    }

    public void Rotate()
    {
        horizontal = !horizontal;
    }
}
