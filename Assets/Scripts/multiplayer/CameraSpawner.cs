using UnityEngine;

public class CameraSpawner : MonoBehaviour
{
    [SerializeField] private GameObject cameraRig;

    void Awake()
    {
        if (cameraRig == null)
            return;

        Instantiate(cameraRig, transform.position, transform.rotation, transform);
    }
}
