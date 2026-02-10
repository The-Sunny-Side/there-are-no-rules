using System.Collections;
using UnityEngine;
using PurrNet;

[RequireComponent(typeof(NetworkManager))]
[DefaultExecutionOrder(-1000)]
public class PurrnetAutoStart : MonoBehaviour
{
    public enum Mode { Host, ServerOnly, Client }

    [Header("TipoAvvio")]
    [SerializeField] private Mode mode = Mode.Host;
    private NetworkManager _nm;

    private void Awake()
    {
        _nm = GetComponent<NetworkManager>();
    }
    private IEnumerator Start()
    {

        Debug.Log($"[PurrNetAutoStart] Starting {mode}");

        if (mode == Mode.Host)
        {
            _nm.StartServer();
            yield return null;
            _nm.StartClient();
        }
        else if (mode == Mode.ServerOnly)
        {
            _nm.StartServer();
        }
        else
        {
            _nm.StartClient();
        }
    }
}
