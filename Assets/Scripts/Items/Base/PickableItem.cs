using Mirror;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PickableItem : NetworkBehaviour
{
    public static GameObject Prefab { get; private set; }
    public UsableItem ContainedItem { get; private set; }

    public static PickableItem Spawn(Vector3 position, UsableItem with = null)
    {
        if (!Prefab) Prefab = Resources.Load<GameObject>("NetworkedPrefabs/PickableItem");

        PickableItem spawned = Instantiate(Prefab, position, Quaternion.identity).GetComponent<PickableItem>();
        if (with) spawned.CmdSetItem(ItemsReader.RegisteredItems.IndexOf(with));

        return spawned;
    }

    [Command(requiresAuthority = false)]
    private void CmdSetItem(int idx)
    {
        ContainedItem = ItemsReader.RegisteredItems[idx];
        RpcSetItem(idx);
    }

    [ClientRpc]
    private void RpcSetItem(int idx)
    {
        ContainedItem = ItemsReader.RegisteredItems[idx];
    }

    private void Awake()
    {
        if (TryGetComponent(out BoxCollider bc))
        {
            bc.isTrigger = true;
        }
    }

    private void FixedUpdate()
    {
        transform.Rotate(Vector3.up, Space.Self);
    }
}
