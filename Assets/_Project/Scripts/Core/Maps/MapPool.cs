using UnityEngine;

namespace Game.Core.Maps
{
    // Both server and clients can access this class safely
    public static class MapPool
    {
        public static MapConfig[] maps;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            maps = Resources.LoadAll<MapConfig>("");
        }
    }
}