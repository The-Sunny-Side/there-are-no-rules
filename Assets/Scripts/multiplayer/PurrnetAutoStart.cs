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
            // Client: cerca server sulla rete
            yield return StartCoroutine(DiscoverServer(foundIP =>
            {
                UDPTransport udpTransport = _nm.GetComponent<UDPTransport>();
                if (udpTransport != null)
                    udpTransport.address = foundIP;

                _nm.StartClient();
            }));
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

    // CLIENT: manda broadcast e aspetta risposta
    private IEnumerator DiscoverServer(Action<string> onFound)
    {
        Debug.Log("[Discovery] Cerco server...");
        string foundIP = null;

        Thread t = new Thread(() =>
        {
            try
            {
                UdpClient client = new UdpClient();
                client.EnableBroadcast = true;
                client.Client.ReceiveTimeout = 3000;

                byte[] data = System.Text.Encoding.UTF8.GetBytes("DISCOVER");
                client.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DISCOVERY_PORT));

                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                byte[] response = client.Receive(ref ep);
                string message = System.Text.Encoding.UTF8.GetString(response);

                if (message.StartsWith("SERVER:"))
                    foundIP = message.Replace("SERVER:", "");

                client.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("[Discovery] Nessun server trovato: " + e.Message);
            }
        });

        t.IsBackground = true;
        t.Start();

        while (t.IsAlive)
            yield return null;

        if (foundIP != null)
        {
            Debug.Log("[Discovery] Server trovato: " + foundIP);
            onFound(foundIP);
        }
        else
        {
            Debug.LogWarning("[Discovery] Nessun server trovato");
        }
    }

    private void OnDestroy()
    {
        _discoverySocket?.Close();
        _discoveryThread?.Abort();
    }
}