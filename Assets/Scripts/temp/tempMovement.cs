using UnityEngine;

public class tempMovement : MonoBehaviour
{
    [SerializeField] private float distance;
    private Vector3 startPosition;
    void Start()
    {
        startPosition=transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (Mathf.Abs(transform.position.z - startPosition.z) > distance)
        {
            transform.position = startPosition;
        }
        else
        {
            transform.Translate(Vector3.forward * Time.deltaTime);
        }

    }
}
