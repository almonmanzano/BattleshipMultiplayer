using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods
{
    public static bool ContainsVector2(this List<Vector2> list, Vector2 vector)
    {
        foreach (Vector2 v in list)
        {
            if (v.x == vector.x && v.y == vector.y)
            {
                return true;
            }
        }

        return false;
    }
}
