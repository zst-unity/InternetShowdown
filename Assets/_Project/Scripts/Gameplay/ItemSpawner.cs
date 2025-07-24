using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.ItemSpawner;
using KinematicCharacterController;
using Mirror;
using UnityEngine;

namespace Game.Gameplay
{
    // This object is only active on the server
    public class ItemSpawner : MonoBehaviour
    {
        public GameObject itemPrefab;
        public float spawnRate;

        private bool _active;
        private float _timer;
        private List<Item> _spawnedItems;

        private void Awake()
        {
            _spawnedItems = new();
            EventBus<SetItemSpawnerActive>.Listen((data) => _active = data.active);
        }

        private void Update()
        {
            if (!_active) return;

            if (MapLoader.loadedMap == null || !MapLoader.loadedMap.scene.IsValid())
            {
                Debug.LogWarning("Item spawner cant function without a loaded map");
                _active = false;
                return;
            }

            _timer += Time.deltaTime;
            if (_timer >= 1f / spawnRate)
            {
                _timer = 0f;
                SpawnItem();
            }

            var playersBounds = new List<Bounds>();
            foreach (var motor in FindObjectsByType<KinematicCharacterMotor>(FindObjectsSortMode.None))
            {
                playersBounds.Add(new
                (
                    motor.transform.position + Vector3.up * motor.Capsule.height / 2f,
                    new(motor.Capsule.radius * 2f, motor.Capsule.height, motor.Capsule.radius * 2f)
                ));
            }

            var cleanupItems = new List<Item>();
            foreach (var item in _spawnedItems)
            {
                foreach (var bounds in playersBounds)
                {
                    if (item.bounds.Intersects(bounds))
                    {
                        NetworkServer.Destroy(item.gameObject);
                        cleanupItems.Add(item);
                    }
                }
            }

            foreach (var item in cleanupItems)
            {
                _spawnedItems.Remove(item);
            }
        }

        private void SpawnItem()
        {
            var minBounds = MapLoader.loadedMap.info.boundsMin;
            var maxBounds = MapLoader.loadedMap.info.boundsMax;
            var x = Random.Range(minBounds.x, maxBounds.x);
            var z = Random.Range(minBounds.z, maxBounds.z);

            var origin = MapLoader.loadedMap.info.transform.position + new Vector3(x, maxBounds.y, z);
            var possibleSpawnPoints = new List<Vector3>();
            while (Physics.Raycast(origin, Vector3.down, out var hit, 200f))
            {
                possibleSpawnPoints.Add(hit.point);
                origin = hit.point + Vector3.down * 0.1f;
            }

            foreach (var point in possibleSpawnPoints)
            {
                var item = Instantiate(itemPrefab, point, Quaternion.identity, new InstantiateParameters() { scene = MapLoader.loadedMap.scene });
                NetworkServer.Spawn(item);
                _spawnedItems.Add(item.GetComponent<Item>());
            }
        }
    }
}