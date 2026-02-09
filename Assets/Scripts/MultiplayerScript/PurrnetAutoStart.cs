using System.Collections;
using UnityEngine;
using PurrNet;

[DefaultExecutionOrder(-1000)]
public class PurrnetAutoStart : MonoBehaviour
{
    public enum Mode { Host, ServerOnly, Client }

    [Header("Avvio")]
    [SerializeField] private Mode mode = Mode.Host;

    [Header("Override (consigliato)")]
    [SerializeField] private NetworkManager networkManagerOverride;

    private IEnumerator Start()
    {
        var nm = networkManagerOverride ? networkManagerOverride : NetworkManager.main;
        if (!nm)
        {
            Debug.LogError("[PurrNetAutoStart] NetworkManager non trovato.");
            yield break;
        }

        // aspetta 1 frame per far inizializzare moduli/scene
        yield return null;

        Debug.Log($"[PurrNetAutoStart] Starting {mode}");

        if (mode == Mode.Host)
        {
            nm.StartServer();
            yield return null; // 1 frame per mettersi in listening
            nm.StartClient();
        }
        else if (mode == Mode.ServerOnly)
        {
            nm.StartServer();
        }
        else // Client
        {
            nm.StartClient();
        }
    }
}
