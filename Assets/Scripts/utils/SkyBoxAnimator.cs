using UnityEngine;

public class SkyBoxAnimator : MonoBehaviour
{
    public float speed = 2f;

    void Update()
    {
        float rot = Time.time * speed;
        RenderSettings.skybox.SetFloat("_Rotation", rot);
    }
}
