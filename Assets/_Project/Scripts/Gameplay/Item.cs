using UnityEngine;

namespace Game.Gameplay
{
    public class Item : MonoBehaviour
    {
        public Vector3 center;
        public Vector3 size;

        public Bounds bounds;

        private void Awake()
        {
            bounds = new(transform.position + center, size);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position + center, size);
        }
    }
}