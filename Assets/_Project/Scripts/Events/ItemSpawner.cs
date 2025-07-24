using Game.Core.Events;

namespace Game.Events.ItemSpawner
{
    public struct SetItemSpawnerActive : IEvent
    {
        public bool active;
    }
}