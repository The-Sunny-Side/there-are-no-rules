using PurrNet;
using UnityEngine;

public class FinishPoint : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("collido"+collision.gameObject.name);
        if (collision.gameObject.CompareTag("PlayerSphere"))
        {       
            CollideWithWinner(collision.gameObject.name);

        }

        //if (collision.gameObject.CompareTag("Player"))
        //{
        //    Debug.Log("Player has reached the finish point!");
        //    // mostrare messaggio a schermo
        //}
    }
     [ObserversRpc]
    void CollideWithWinner(string  name)
    {
        if(isServer)
            Debug.Log("hai vinto"+ name);
    }
    protected override void OnSpawned(bool asServer)
    {
        if (asServer)
            return;
        Debug.Log("nasco");
    }

    [ServerRpc]
    private void TestServerMethod()
    {
        //This code will be called on the server.

        //The server is now calling the observers rpc method on all observing clients.
        //If your Network Rules allow for it, this could also be called from clients, essentially skipping the Server RPC
        TestObserverMethod();
    }

    [ObserversRpc]
    private void TestObserverMethod()
    {
        //This code will be called on every observing clients machine.
        //This is a good way to update all observing clients with the same information.
    }
}
