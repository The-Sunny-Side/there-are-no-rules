using UnityEngine;
using System.Collections.Generic;

public static class GameObjectExtensions
{
    public static GameObject FindChildWithTag(this GameObject parent, string tag)
    {
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            if (child.CompareTag(tag))
            {
                return child.gameObject;
            }
        }

        return null;
    }

    public static GameObject FindChildWithName(this GameObject parent, string name)
    {
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            if (child.name==name)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    public static GameObject[] FindChildrenWithTag(this GameObject parent, string tag)
    {
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>(true);
        List<GameObject> result = new List<GameObject>();

        foreach (Transform child in allChildren)
        {
            if (child.CompareTag(tag))
            {
                result.Add(child.gameObject);
            }
        }

        return result.ToArray();
    }
}