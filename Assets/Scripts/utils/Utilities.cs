using UnityEngine;

public static class Utilities
{
    public static void DestroyAllChildren(GameObject source)
    {
        foreach (Transform child in source.transform)
        {
            Object.Destroy(child.gameObject);
            
        }
    }

    public static void DestroyAllChildren(Transform source)
    {
        foreach (Transform child in source)
        {
            Object.Destroy(child.gameObject);

        }
    }
}