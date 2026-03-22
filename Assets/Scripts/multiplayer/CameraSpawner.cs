using UnityEngine;

public class CameraSpawner : MonoBehaviour
{
    [SerializeField] private GameObject cameraPrefab;

    void Awake()
    {
        if (cameraPrefab == null)
            return;

        var instance = Instantiate(cameraPrefab, transform.position, transform.rotation, transform);
        instance.name = cameraPrefab.name;
    }
}
