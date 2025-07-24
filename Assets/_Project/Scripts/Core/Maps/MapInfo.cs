using UnityEngine;

namespace Game.Core.Maps
{
    public class MapInfo : MonoBehaviour
    {
        public Transform[] spawnPoints;
        public Vector3 boundsMin;
        public Vector3 boundsMax;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + (boundsMin + boundsMax) / 2f, boundsMax - boundsMin);
        }
    }
}