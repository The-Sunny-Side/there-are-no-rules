using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;

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

    public static void DefaultCallback(){}
    public static void DefaultCallback(string p) { }
    public static void DefaultCallback(bool p) { }
    public static void DefaultCallback(int p) { }

    public static IEnumerator DelayedEvent(UnityAction callback = null, float timeout = 0.5f)
    {
        yield return new WaitForSeconds(timeout);
        callback();
    }
}
