using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class ServerDiscoveryService : MonoBehaviour
{
    private Thread _discoveryThread;

    [SerializeField]
    private int discoveryPort = 47777;

    [SerializeField]
    private int timeout = 5000;

    [Header("On Success callback")]
    public UnityEvent<string> OnSuccess;

    [Header("On Failure callback")]
    public UnityEvent OnFailure;

    private string serverIp;

    private IEnumerator Start()
    {
        // Client: cerca server sulla rete
        yield return StartCoroutine(DiscoverServer(foundIP =>
        {
            if (foundIP == "NOT_FOUND")
            {
                OnFailure?.Invoke();
            }
            else
            {
                OnSuccess?.Invoke(foundIP);
                GameManager.Instance.SetIpAddress(foundIP);
                GameManager.Instance.SetNetworkMode(Mode.Client);

                GameManager.Instance.GoToPlayScreen();
            }
        }));

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
                client.Client.ReceiveTimeout = timeout;

                byte[] data = System.Text.Encoding.UTF8.GetBytes("DISCOVER");
                client.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, discoveryPort));

                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                byte[] response = client.Receive(ref ep);
                string message = System.Text.Encoding.UTF8.GetString(response);

                if (message.StartsWith("SERVER:"))
                {
                    Debug.Log("[Discovery] Server trovato: " + foundIP);
                    foundIP = message.Replace("SERVER:", "");
                }

                client.Close();
            }
            catch (Exception e)
            {
                Debug.Log("[Discovery] Nessun server trovato: " + e.Message);
            }
        });

        t.IsBackground = true;
        t.Start();

        while (t.IsAlive)
            yield return null;

        if (foundIP != null)
        {
            onFound(foundIP);
        }
        else
        {
            Debug.LogWarning("[Discovery] Nessun server trovato");
            onFound("NOT_FOUND");
        }
    }

    private void OnDestroy()
    {
        _discoveryThread?.Abort();
    }
}
