using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using PurrNet;
using PurrNet.Transports;

[RequireComponent(typeof(NetworkManager))]
[DefaultExecutionOrder(-1000)]
public class PurrnetAutoStart : MonoBehaviour
{
    private NetworkManager _nm;
    private UdpClient _discoverySocket;
    private Thread _discoveryThread;
    private const int DISCOVERY_PORT = 47777;

    private void Awake()
    {
        _nm = GetComponent<NetworkManager>();
        if (_nm != null)
        {
            UDPTransport udpTransport = _nm.GetComponent<UDPTransport>();
            if (udpTransport != null)
            {
                udpTransport.address = GameManager.Instance.GetIpAddress();
                Debug.Log(udpTransport.address);
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
            StartDiscoveryListener(); // server risponde ai client
        }
        else if (GameManager.Instance.GetNetworkMode() == Mode.ServerOnly)
        {
            _nm.StartServer();
            StartDiscoveryListener(); // server risponde ai client
        }
        else
        {
            _nm.StartClient();
        }
    }

    // SERVER: ascolta DISCOVER e risponde con il proprio IP
    private void StartDiscoveryListener()
    {
        _discoverySocket = new UdpClient(DISCOVERY_PORT);
        _discoveryThread = new Thread(() =>
        {
            string localIP = GameManager.Instance.GetIpAddress();
            byte[] response = System.Text.Encoding.UTF8.GetBytes("SERVER:" + localIP);

            while (true)
            {
                try
                {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _discoverySocket.Receive(ref remote);
                    string message = System.Text.Encoding.UTF8.GetString(data);

                    if (message == "DISCOVER")
                    {
                        _discoverySocket.Send(response, response.Length, remote);
                        Debug.Log($"[Discovery] Risposto a {remote.Address}");
                    }
                }
                catch { break; }
            }
        });
        _discoveryThread.IsBackground = true;
        _discoveryThread.Start();
    }

    private void OnDestroy()
    {
        _discoverySocket?.Close();
        _discoveryThread?.Abort();
    }
}