using PurrNet;
using PurrNet.Modules;
using UnityEngine;

public class VehicleConfigResync : PurrMonoBehaviour
{
    public override void Subscribe(NetworkManager manager, bool asServer)
    {
        if (!asServer) return;

        if (manager.TryGetModule(out ScenePlayersModule scenePlayersModule, true))
            scenePlayersModule.onPlayerLoadedScene += OnPlayerLoadedScene;
    }

    public override void Unsubscribe(NetworkManager manager, bool asServer)
    {
        if (!asServer) return;

        if (manager.TryGetModule(out ScenePlayersModule scenePlayersModule, true))
            scenePlayersModule.onPlayerLoadedScene -= OnPlayerLoadedScene;
    }

    private void OnPlayerLoadedScene(PlayerID player, SceneID scene, bool asServer)
    {
        if (!asServer) return;

        // quando entra un nuovo player: ribroadcast delle config già note
        var all = Object.FindObjectsByType<VehicleNetConfig>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var v in all)
            v.ServerResendIfAny();
    }
}