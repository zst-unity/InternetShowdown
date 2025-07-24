using Mirror;
using UnityEngine;

namespace Game.Network.Messages
{
    public struct ClientRequestMapLoad : NetworkMessage { }
    public struct ServerMovePlayer : NetworkMessage
    {
        public Vector3 position;
    }
    public struct ServerConfirmPlayerEnteredMatch : NetworkMessage { }
}