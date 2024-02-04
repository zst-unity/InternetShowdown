using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class ItemsReader : NetworkBehaviour
{
    [Header("Objects")]
    [SerializeField] private GameObject _pickupParticle;
    [SerializeField] private SoundEffect _pickupSound;

    [Header("Other")]
    [SerializeField] private byte _luckModifier = 0;
    [SerializeField] private Transform _itemHolder;

    public bool HasItem { get => _currentItem != null; }

    private UsableItem _currentItem;
    private NetworkPlayer _player;

    public List<Mutation> ActiveMutations = new();

    private int _itemsUsed;

    public static List<UsableItem> RegisteredItems { get; private set; } = new();

    private void Awake()
    {
        RegisteredItems = Resources.LoadAll<UsableItem>("Items").ToList();
    }

    private void Start()
    {
        _player = GetComponent<NetworkPlayer>();

        if (!isLocalPlayer) return;
        _itemHolder.SetParent(_player.PlayerCamera.transform);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isLocalPlayer) return;

        if (other.TryGetComponent(out PickableItem pickedItem))
        {
            if (!HasItem)
            {
                CmdOnItemPickup(other.gameObject, other.transform.position);
                _player.PlayerMoveCamera.Shake();

                SoundSystem.Singleton.PlaySFX
                (
                    new SoundTransporter(_pickupSound.Sounds),
                    new SoundPositioner(transform.position),
                    _pickupSound.Pitch.x, _pickupSound.Pitch.y,
                    _pickupSound.Volume
                );

                if (pickedItem.ContainedItem)
                    SetCurrentItem(pickedItem.ContainedItem);
                else
                    GetItem();

            }
        }
    }

    [Command]
    private void CmdOnItemPickup(GameObject item, Vector3 position)
    {
        GameObject newPickupParticle = Instantiate(_pickupParticle, position, Quaternion.identity);
        NetworkServer.Spawn(newPickupParticle);

        ItemSpawner.Singleton.Destroy(item);
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        CheckForItem();
    }

    private void CheckForItem()
    {
        if (!HasItem) return;

        if (!_player.AllowMovement || PauseMenu.Singleton.PauseMenuOpened) return;

        bool holdToUse = _currentItem.HoldToUse;

        if (holdToUse)
        {
            if (Input.GetMouseButtonDown(0))
            {
                float useTime = _currentItem.UseTime;

                Invoke(nameof(UseItem), useTime);
                Hud.Singleton.StartInteraction(useTime);
            }

            if (Input.GetMouseButtonUp(0))
            {
                CancelInvoke(nameof(UseItem));
                Hud.Singleton.StopInteraction();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0)) UseItem();
        }
    }

    public void UseItem()
    {
        if (!HasItem)
        {
            Debug.LogWarning("Can't use item because it's NULL");
            return;
        }

        if (!_player.AllowMovement) return;

        foreach (InspectorMutation insMutation in _currentItem.Mutations)
        {
            Mutation mutation = MutationJobs.InspectorToMutation(insMutation);

            mutation.Mutate();
            ActiveMutations.Add(mutation);

            StartCoroutine(nameof(CancelMutationFromList), mutation);
        }

        foreach (var proj in _currentItem.Projectiles)
        {
            SpawnProjectile(proj);
        }

        if (_currentItem.HealAmount > 0)
            _player.Heal(_currentItem.HealAmount);
        else if (_currentItem.HealAmount < 0)
            _player.TakeDamage(Mathf.Abs(_currentItem.HealAmount));

        LoseItem();
        _player.OnItemUsed();
    }

    private IEnumerator CancelMutationFromList(Mutation mutation)
    {
        yield return new WaitForSeconds(mutation.Time);

        ActiveMutations.Remove(mutation);
    }

    public void RemoveAllMutations()
    {
        foreach (var mutation in ActiveMutations)
        {
            mutation.CancelMutation();
        }

        PlayerMutationStats.Singleton.ResetStats();
    }

    public void LoseItem()
    {
        if (!HasItem)
        {
            Debug.LogWarning("Can not lose NULL item");
            return;
        }

        StartCoroutine(nameof(LoseItemCoroutine));
    }

    private IEnumerator LoseItemCoroutine()
    {
        yield return new WaitUntil(() => _itemHolder.childCount > 0);

        SetCurrentItem(null);
    }

    public void GetItem()
    {
        PlayerCurrentStats.Singleton.Luck = _luckModifier;

        List<UsableItem> sortedItems;
        Rarity closestRarity;

        (sortedItems, closestRarity) = Generate();

        while (RarityJobs.GetAllWithRarity(sortedItems, closestRarity).Count == 0)
        {
            (sortedItems, closestRarity) = Generate();
        }

        List<UsableItem> chosenCategory = new();

        foreach (var item in sortedItems)
        {
            if (item.ItemRarity == closestRarity) chosenCategory.Add(item);
        }

        SetCurrentItem(chosenCategory[UnityEngine.Random.Range(0, chosenCategory.Count)]);
    }

    private (List<UsableItem> sortedItems, Rarity closestRarity) Generate()
    {
        byte choice = RarityJobs.Select((byte)(PlayerCurrentStats.Singleton.Luck + PlayerMutationStats.Singleton.Luck));

        List<UsableItem> sortedItems = RarityJobs.Sort(RegisteredItems).ToList();

        var closestRarity = RarityJobs.Rarities.ToList();
        closestRarity.Sort((first, second) => second.Value > choice ? 1 : -1);

        Rarity convertedClosestRarity = RarityJobs.KeyValuePairToRarity(closestRarity.First());

        return (sortedItems, convertedClosestRarity);
    }

    private void MakeVisual(GameObject visual, float zOffset = 0)
    {
        ClearItemHolder();

        GameObject newVisual = Instantiate(visual, _itemHolder);
        newVisual.transform.localPosition += transform.forward * zOffset;
    }

    private void ClearItemHolder()
    {
        foreach (Transform item in _itemHolder)
        {
            Destroy(item.gameObject);
        }
    }

    private void OnCurrentItemChange()
    {
        if (HasItem)
            MakeVisual(_currentItem.ItemVisual, isLocalPlayer ? 0 : 1);
        else
            ClearItemHolder();
    }

    private void SpawnProjectile(ProjectileBase proj)
    {
        Transform cameraTransform = _player.PlayerCamera.transform;

        CmdSpawnProjectile(_currentItem.Projectiles.IndexOf(proj), transform.position + cameraTransform.forward, cameraTransform.rotation, connectionToClient);
    }

    #region NETWORK

    [Command]
    private void CmdSpawnProjectile(int idx, Vector3 pos, Quaternion dir, NetworkConnectionToClient connection)
    {
        ItemsReader client = connection.identity.GetComponent<ItemsReader>();

        if (client._currentItem == null)
        {
            Debug.LogWarning("Cannot spawn projectile from NULL item");
            return;
        }

        GameObject newProjectile = Instantiate(client._currentItem.Projectiles[idx].gameObject, pos, dir);
        NetworkServer.Spawn(newProjectile, connection);

        RpcOnProjectileSpawned(newProjectile);
    }

    [ClientRpc]
    private void RpcOnProjectileSpawned(GameObject proj) { }

    private void SetCurrentItem(UsableItem target)
    {
        RegisteredItems.Sort((first, second) => (byte)first.ItemRarity < (byte)second.ItemRarity ? -1 : 1);

        _currentItem = target;

        if (target == null)
            CmdSetCurrentItem(null);
        else
            CmdSetCurrentItem(RegisteredItems.IndexOf(target));
    }

    [Command]
    private void CmdSetCurrentItem(int? idx)
    {
        RegisteredItems.Sort((first, second) => (byte)first.ItemRarity < (byte)second.ItemRarity ? -1 : 1);

        SetItemOnClient(idx);
        RpcSetCurrentItem(idx);
    }

    [ClientRpc]
    private void RpcSetCurrentItem(int? idx)
    {
        RegisteredItems.Sort((first, second) => (byte)first.ItemRarity < (byte)second.ItemRarity ? -1 : 1);

        SetItemOnClient(idx);
        OnCurrentItemChange();
    }

    private void SetItemOnClient(int? idx)
    {
        if (idx == null)
            _currentItem = null;
        else
            _currentItem = RegisteredItems[idx.Value];
    }

    #endregion
}
