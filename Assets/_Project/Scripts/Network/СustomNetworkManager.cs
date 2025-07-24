using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.Gameplay;
using Game.Network.Messages;
using Game.Player;
using Mirror;
using UnityEngine;

namespace Game.Network
{
    public class CustomNetworkManager : NetworkManager
    {
        public static CustomNetworkManager CustomSingleton => (CustomNetworkManager)singleton;
        private GameObject _portal;

        public override void OnStartServer()
        {
            MapLoader.Init();
            NetworkServer.RegisterHandler<ClientRequestMapLoad>((conn, _) =>
            {
                if (!MapLoader.TryMoveGameObjectToMap(conn.identity.gameObject))
                {
                    Debug.LogWarning($"Client {conn.connectionId} wanted to load into unloaded map");
                    return;
                }

                conn.Send(new SceneMessage() { sceneName = MapLoader.loadedMap.config.sceneName, sceneOperation = SceneOperation.LoadAdditive });
                var position = MapLoader.loadedMap.info.spawnPoints[Random.Range(0, MapLoader.loadedMap.info.spawnPoints.Length)].position;
                conn.Send<ServerMovePlayer>(new() { position = position });
                conn.Send<ServerConfirmPlayerEnteredMatch>(new());
            });
        }

        public override void OnStopServer()
        {
            MapLoader.Stop();
        }

        public override void OnStartClient()
        {
            NetworkClient.RegisterHandler<ServerMovePlayer>((data) =>
            {
                NetworkClient.localPlayer.GetComponent<PlayerMovement>().SetPosition(data.position);
            });

            NetworkClient.RegisterHandler<ServerConfirmPlayerEnteredMatch>((data) =>
            {
                EventBus<RequestMatchMusic>.Invoke(new());
            });

            EventBus<OnGameStateChange>.Listen((data) =>
            {
                if (!_portal) _portal = GameObject.FindGameObjectWithTag("Portal");
                _portal.SetActive(data.state.isMatch);
                _portal.GetComponent<MeshRenderer>().enabled = data.state.isMatch; // mirror for some reason automaticly disables mesh renderer
            });
        }
    }
}