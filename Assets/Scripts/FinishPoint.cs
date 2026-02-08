using PurrNet;
using System.Collections.Generic;
using UnityEngine;

public class FinishPoint : NetworkBehaviour
{
    [SerializeField] private NetworkManager netManager;
    public List<PlayerID> playerList = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        netManager.onPlayerJoined += UpdateList;
    }

    private void UpdateList(PlayerID player, bool isreconnect, bool asserver)
    {
        playerList.Add(player);
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
            PlayerID? winningId = collision.gameObject.GetComponent<NetworkTransform>().localPlayer;
            Congrats(winningId.GetValueOrDefault());
            foreach (var player in playerList)
            {
                if (player != winningId)
                {

                    LoseMessage(player);

                }
            }
        }
    }

    [TargetRpc]
    private void Congrats(PlayerID target)
    {
        if (target != null)
        {
            Debug.Log("hai vinto");
        }
    }

    [TargetRpc]
    private void LoseMessage(PlayerID target)
    {
        if (target != null)
        {
            Debug.Log("hai perso sarà per la prossima");
        }
    }
}
