using UnityEngine;

public class SkyBoxAnimator : MonoBehaviour
{
    public float speed = 2f;
    private Material _skyboxInstance;

    void Start()
    {
        // Crea una copia in memoria: l'asset su disco non viene mai toccato
        _skyboxInstance = new Material(RenderSettings.skybox);
        RenderSettings.skybox = _skyboxInstance;
    }

    void Update()
    {
        _skyboxInstance.SetFloat("_Rotation", Time.time * speed);
    }

    void OnDestroy()
    {
        // Pulizia: evita memory leak nell'editor
        if (_skyboxInstance != null)
            Destroy(_skyboxInstance);
    }
}