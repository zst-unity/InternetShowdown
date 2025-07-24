using Mirror;
using UnityEngine;

namespace Game.Core.Maps
{
    [CreateAssetMenu(fileName = "MapConfig", menuName = "Map Config", order = 0)]
    public class MapConfig : ScriptableObject
    {
        [Scene] public string sceneName;
        public string displayName;
        public AudioClip[] soundtracks;
    }
}