using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private float offset=5;
    public GameObject[] players;
    
    void Start()
    {
        for (int i = 0; i < players.Length; i++)
        {
            Vector3 spawnPosition = new Vector3(transform.position.x + i * offset, transform.position.y, transform.position.z);
            Instantiate(players[i], spawnPosition, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
