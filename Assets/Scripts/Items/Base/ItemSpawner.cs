using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class ItemSpawner : NetworkBehaviour
{
    [SerializeField, Min(0)] private int _maxSpawnAmount = 50;
    [SerializeField, Min(0.1f)] private float _spawnRate = 4f;

    [Space(9)]

    [SerializeField] private Vector3 _bounds = new Vector3(1, 1, 1);

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
        PickableItem[] all = FindObjectsOfType<PickableItem>(true);

        foreach (var item in all)
        {
            NetworkServer.Destroy(item.gameObject);
        }
    }

    public void SpawnItem()
    {
        PickableItem[] allSpawned = FindObjectsOfType<PickableItem>();

        if (allSpawned.Length >= _maxSpawnAmount) return;

        Vector3 randomPlace = new(
            Random.Range(-(_bounds.x / 2), _bounds.x / 2),
            Random.Range(-(_bounds.y / 2), _bounds.y / 2),
            Random.Range(-(_bounds.z / 2), _bounds.z / 2)
        );

        NavMesh.SamplePosition(randomPlace, out NavMeshHit hit, randomPlace.magnitude, 1);

        GameObject spawnedItem = PickableItem.Spawn(hit.position + Vector3.up * 1.5f).gameObject;
        NetworkServer.Spawn(spawnedItem);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = ColorISH.Cyan;
        Gizmos.DrawWireCube(Vector3.zero, _bounds);
    }
}
