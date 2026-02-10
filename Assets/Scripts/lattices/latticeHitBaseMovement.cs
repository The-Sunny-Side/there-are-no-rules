using UnityEngine;

public class latticeHitBaseMovement : MonoBehaviour
{
    [SerializeField] private float speed=5;
    void Start()
    {
        Destroy(gameObject, 2f);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward*Time.deltaTime*speed);
    }
}
