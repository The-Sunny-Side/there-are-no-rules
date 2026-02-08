using PurrNet;
using System.Collections.Generic;
using PurrNet.Packing;
using UnityEngine;

public class FinishPoint : NetworkBehaviour
{
    [SerializeField] private NetworkManager netManager;
    private List<PlayerID> playerList = new();

    public List<PackedULong> playerIdList = new();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        netManager.onPlayerJoined += UpdateList;
        netManager.onPlayerLeft += RemovePlayer;
    }

    private void RemovePlayer(PlayerID player, bool asserver)
    {
        playerList.Remove(player);
        playerIdList.Remove(player.id);
    }

    private void UpdateList(PlayerID player, bool isreconnect, bool asserver)
    {
        playerList.Add(player);
        playerIdList.Add(player.id);
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
                if (!player.Equals(winningId) )
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
