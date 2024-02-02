using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class ItemSpawner : NetworkBehaviour
{
    [SerializeField, Min(0)] private int _maxSpawnAmount = 50;
    [SerializeField, Min(0.1f)] private float _spawnRate = 4f;

    [Space(9)]

    [SerializeField] private Vector3 _bounds = new Vector3(1, 1, 1);

    private Dictionary<Vector3, GameObject> _spawnedItems = new();

    public static ItemSpawner Singleton { get; private set; }

    private void Awake()
    {
        Singleton = this;
    }

    [ServerCallback]
    public void StartSpawnProcess()
    {
        InvokeRepeating(nameof(SpawnItem), 0f, _spawnRate);
    }

    [ServerCallback]
    public void StopSpawnProcess()
    {
        CancelInvoke(nameof(SpawnItem));
    }

    [ServerCallback]
    public void DestroyAll()
    {
        foreach (var item in _spawnedItems.Values)
        {
            NetworkServer.Destroy(item);
        }

        _spawnedItems.Clear();
    }

    [ServerCallback]
    public void Destroy(GameObject item)
    {
        _spawnedItems.Remove(item.transform.position);
        NetworkServer.Destroy(item);
    }

    private void SpawnItem()
    {
        if (_spawnedItems.Count >= _maxSpawnAmount) return;

        Vector3 pos;
        do
        {
            Vector3 randomPlace = new
            (
                Random.Range(-(_bounds.x / 2), _bounds.x / 2),
                Random.Range(-(_bounds.y / 2), _bounds.y / 2),
                Random.Range(-(_bounds.z / 2), _bounds.z / 2)
            );

            NavMesh.SamplePosition(randomPlace, out NavMeshHit hit, randomPlace.magnitude, 1);
            pos = hit.position + Vector3.up * 1.5f;
        } while (_spawnedItems.ContainsKey(pos));

        GameObject spawnedItem = PickableItem.Spawn(pos).gameObject;
        NetworkServer.Spawn(spawnedItem);
        _spawnedItems.Add(spawnedItem.transform.position, spawnedItem);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = ColorISH.Cyan;
        Gizmos.DrawWireCube(Vector3.zero, _bounds);
    }
}
