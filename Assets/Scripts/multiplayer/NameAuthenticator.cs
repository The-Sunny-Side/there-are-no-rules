using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet.Authentication;
using PurrNet.Transports;

// Trasporta il display name del player nel payload di autenticazione PurrNet.
// Il server lo riceve prima che onPlayerJoined scatti, quindi LobbyState
// ha già il nome corretto al momento del join (niente race, niente RPC di follow-up).
public class NameAuthenticator : AuthenticationBehaviour<string>
{
    private static readonly Dictionary<Connection, string> _names = new();

    public static bool TryGetName(Connection conn, out string name) => _names.TryGetValue(conn, out name);

    protected override Task<AuthenticationRequest<string>> GetClientPayload()
    {
        string name = GameConfig.Data?.name ?? string.Empty;
        return Task.FromResult(new AuthenticationRequest<string>(name));
    }

    protected override Task<AuthenticationResponse> ValidateClientPayload(Connection conn, string payload)
    {
        _names[conn] = payload ?? string.Empty;
        return Task.FromResult<AuthenticationResponse>(true);
    }

    protected override void UnAuthenticateClient(Connection conn)
    {
        _names.Remove(conn);
    }
}
