using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public static class Utilities
{
    public static void DestroyAllChildren(GameObject source)
    {
        if (source == null)
            return;

        foreach (Transform child in source.transform)
        {
            UnityEngine.Object.Destroy(child.gameObject);
            
        }
    }

    public static void DestroyAllChildren(Transform source)
    {
        if (source == null)
            return;

        foreach (Transform child in source)
        {
            UnityEngine.Object.Destroy(child.gameObject);

        }
    }

    public static string GetLocalIPAddress()
    {
        string localIP = "Non trovato";
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Errore IP: " + e.Message);
        }
        return localIP;
    }
}
