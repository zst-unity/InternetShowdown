using Game.Core.Events;
using Game.Gameplay;

namespace Game.Events.Gameplay
{
    public struct OnGameStateChange : IEvent
    {
        public GameState state;
    }
}