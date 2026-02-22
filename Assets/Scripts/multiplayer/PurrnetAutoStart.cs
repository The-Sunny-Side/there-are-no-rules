using System.Collections;
using UnityEngine;
using PurrNet;
using PurrNet.Transports;

[RequireComponent(typeof(NetworkManager))]
[DefaultExecutionOrder(-1000)]
public class PurrnetAutoStart : MonoBehaviour
{

    private NetworkManager _nm;

    private void Awake()
    {
        _nm = GetComponent<NetworkManager>();
        if (_nm != null)
        {
            UDPTransport udpTransport = _nm.GetComponent<UDPTransport>();
            if (udpTransport != null)
            {

                udpTransport.address = GameManager.Instance.GetIpAddress();
            }
        }
    }
    private IEnumerator Start()
    {

        Debug.Log($"[PurrNetAutoStart] Starting {GameManager.Instance.GetNetworkMode()}");

        if (GameManager.Instance.GetNetworkMode() == Mode.Host)
        {
            _nm.StartServer();
            yield return null;
            _nm.StartClient();
        }
        else if (GameManager.Instance.GetNetworkMode() == Mode.ServerOnly)
        {
            _nm.StartServer();
        }
        else
        {
            _nm.StartClient();
        }
    }
}
