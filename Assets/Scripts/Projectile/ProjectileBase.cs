using System;
using Mirror;
using Mirror.Experimental;
using NaughtyAttributes;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(NetworkRigidbody))]
[RequireComponent(typeof(NetworkTransformReliable))]
public class ProjectileBase : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] protected Rigidbody _rb;
    [SerializeField] protected NetworkRigidbody _nrb;
    [SerializeField] protected NetworkRigidbody _nt;

    [Header("Behaviour Settings")]
    [SerializeField, Tooltip("Из-за чего должен удалиться снаряд?"), EnumFlags] protected HitDestroy _destroyMode;
    [SerializeField, Tooltip("После скольких секунд снаряд удалится?"), ShowIf(nameof(_destroyMode), HitDestroy.OnTime), AllowNesting] protected float _destroyTime = 3f;
    [SerializeField, Tooltip("После скольких столкновений снаряд удалится?"), ShowIf(nameof(_destroyMode), HitDestroy.OnCollide), AllowNesting, Min(1)] protected int _destroyHits = 1;
    [SerializeField, ShowIf(nameof(_destroyMode), HitDestroy.OnRadiusDamage), AllowNesting, Min(0)] protected float _startHealth = 10f;
    [SerializeField, ShowIf(nameof(_destroyMode), HitDestroy.OnRadiusDamage), AllowNesting, Min(0)] protected float _minRadius = 0f;
    [SerializeField, ShowIf(nameof(_destroyMode), HitDestroy.OnRadiusDamage), AllowNesting, Min(1)] protected bool _requiredToBeExplosion = true;
    [SerializeField, ShowIf(nameof(_destroyMode), HitDestroy.OnRadiusDamage), AllowNesting, Min(0)] protected float _delay = 0.25f;

    private float _projectileHealth;

    [Header("Force Settings")]
    [SerializeField, Tooltip("Скорость снаряда")] protected float _projectileSpeed = 10;
    [SerializeField, Tooltip("Урон снаряда")] protected float _projectileDamage = 10;

    [Space(9)]

    [SerializeField, Tooltip("Как будет применяться скорость снаряду? Через _rb.velocity = force, или через _rb.AddForce(force)?")] protected ForceApplyMode _forceApplyMode = ForceApplyMode.SetForce;
    [SerializeField, Tooltip("Снаряду будет постоянно применяться скорость, или только тогда, когда он заспавнился?")] protected bool _continuousForceApply = false;

    [Header("Effect Settings")]
    [SerializeField, Tooltip("Какие и когда звуки должны проигрываться?"), EnumFlags] protected EffectModes _soundEffects;
    [SerializeField, Tooltip("Какие и когда звуки должны проигрываться?"), EnumFlags] protected EffectModes _particleEffects;
    [SerializeField, Tooltip("Какие и когда звуки должны проигрываться?"), EnumFlags] protected EffectModes _shakeEffects;

    [Header("Sound Effects")]
    [SerializeField, Tooltip("Звук спавна"), ShowIf(nameof(_soundEffects), EffectModes.OnSpawn), AllowNesting] protected SoundEffect _spawnSound;
    [SerializeField, Tooltip("Звук столкновения"), ShowIf(nameof(_soundEffects), EffectModes.OnCollide), AllowNesting] protected SoundEffect _collideSound;
    [SerializeField, Tooltip("Звук деспавна"), ShowIf(nameof(_soundEffects), EffectModes.OnDestroy), AllowNesting] protected SoundEffect _destroySound;

    [Header("Particle Effects")]
    [SerializeField, Tooltip("Партикл спавна"), ShowIf(nameof(_particleEffects), EffectModes.OnSpawn), AllowNesting] protected GameObject _spawnEffect;
    [SerializeField, Tooltip("Партикл столкновения"), ShowIf(nameof(_particleEffects), EffectModes.OnCollide), AllowNesting] protected GameObject _collideEffect;
    [SerializeField, Tooltip("Партикл деспавна"), ShowIf(nameof(_particleEffects), EffectModes.OnDestroy), AllowNesting] protected GameObject _destroyEffect;

    [Header("Shake Effects")]
    [SerializeField, Tooltip("Тряска экрана при спавне"), ShowIf(nameof(_shakeEffects), EffectModes.OnSpawn), AllowNesting] protected ShakeEffect _spawnShake;
    [SerializeField, Tooltip("Тряска экрана при столкновении"), ShowIf(nameof(_shakeEffects), EffectModes.OnCollide), AllowNesting] protected ShakeEffect _collideShake;
    [SerializeField, Tooltip("Тряска экрана при деспавне"), ShowIf(nameof(_shakeEffects), EffectModes.OnDestroy), AllowNesting] protected ShakeEffect _destroyShake;

    private Vector3 _targetDirection;
    private int _collisionCount;

    protected virtual void OnCollide(int layer, Vector3 velocity, ContactPoint contactPoint) { } // вызывается когда снаряд касается чего либо (в параметр возвращает слой объекта)
    protected virtual void OnHitPlayer(Vector3 velocity) { } // вызывается когда снаряд касается игрока
    protected virtual void OnHitMap(Vector3 velocity, ContactPoint contactPoint) { } // вызывается когда снаряд касается карты
    protected virtual void OnRadiusDamageInteraction(float radius, float damage, bool explosion) { }

    protected virtual void OnInit() { } // вызывается когда снаряд инициализируется
    protected virtual void OnTime() { } // вызывается так же как и FixedUpdate

    private void OnValidate()
    {
        GetAndSetComponents();
        CheckEffects();
    }

    private void GetAndSetComponents()
    {
        if (TryGetComponent<Rigidbody>(out _rb))
        {
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        if (TryGetComponent<NetworkRigidbody>(out _nrb))
        {
            _nrb.syncDirection = SyncDirection.ClientToServer;
            _nrb.clientAuthority = true;
        }

        if (TryGetComponent(out _nt))
        {
            _nt.syncDirection = SyncDirection.ClientToServer;
        }
    }

    private void CheckEffects()
    {
        CheckEffectForNetworkIdentity(ref _spawnEffect);
        CheckEffectForNetworkIdentity(ref _collideEffect);
        CheckEffectForNetworkIdentity(ref _destroyEffect);
    }

    private void CheckEffectForNetworkIdentity(ref GameObject target)
    {
        if (target && !target.GetComponent<NetworkIdentity>())
        {
            Debug.LogWarning($"{target.name} has no NetworkIdentity attached");
            target = null;
        }
    }

    private void Start()
    {
        OnInit(); // вызов калбека для кастомного повидения

        _projectileHealth = _startHealth;
        _targetDirection = transform.forward;
        gameObject.layer = 10;

        ApplyForce();

        if (!isOwned) return;

        if (_destroyMode.HasFlag(HitDestroy.OnTime))
        {
            Invoke(nameof(DestroySelf), _destroyTime); // инвок вызывает метод через время
        }

        CheckForEffects(EffectModes.OnSpawn, _spawnSound, _spawnEffect, _spawnShake);
    }

    private void FixedUpdate()
    {
        OnTime(); // вызов калбека для кастомного повидения

        if (_continuousForceApply) ApplyForce();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!isOwned) return;

        _collisionCount++;

        OnCollide(other.gameObject.layer, _rb.velocity, other.contacts[0]); // вызов калбека для кастомного повидения

        //Проверки и уничтожение
        CheckForEffects(EffectModes.OnCollide, _collideSound, _collideEffect, _collideShake);
        if (_collisionCount == _destroyHits) CheckForDestroy(HitDestroy.OnCollide);

        if (other.gameObject.layer == 11)
        {
            OnHitPlayer(_rb.velocity); // вызов калбека для кастомного повидения

            HitPlayer(other.gameObject);
        }

        if (other.gameObject.layer == 6)
        {
            OnHitMap(_rb.velocity, other.contacts[0]); // вызов калбека для кастомного повидения

            CheckForDestroy(HitDestroy.OnMap);
        }
    }

    private void ApplyForce()
    {
        if (!isOwned) return;

        bool isSetForce = _forceApplyMode == ForceApplyMode.SetForce;

        Vector3 targetForce = _targetDirection * _projectileSpeed;

        if (isSetForce)
        {
            _rb.velocity = targetForce;
        }

        else if (!isSetForce)
        {
            _rb.AddForce(targetForce);
        }
    }

    private void HitPlayer(GameObject player)
    {
        if (!player.TryGetComponent(out NetworkPlayer toHit))
        {
            Debug.Log("The object you trying to hit isn't a player");
            return;
        }

        PlayerCurrentStats.Singleton.Damage = _projectileDamage;
        toHit.CmdHitPlayer(NetworkClient.localPlayer, _projectileDamage + PlayerMutationStats.Singleton.Damage);

        CheckForDestroy(HitDestroy.OnPlayer);
    }

    private void DestroySelf()
    {
        CheckForEffects(EffectModes.OnDestroy, _destroySound, _destroyEffect, _destroyShake);
        CmdDestroySelf();
    }

    private void CheckForDestroy(HitDestroy hitDestroy)
    {
        if (_destroyMode.HasFlag(hitDestroy)) DestroySelf();
    }

    private void CheckForEffects(EffectModes effectValue, SoundEffect sound, GameObject effect, ShakeEffect shake)
    {
        if (_soundEffects.HasFlag(effectValue)) PlayProjectileSound(sound);
        if (_particleEffects.HasFlag(effectValue)) SpawnProjectileEffect(effect);
        if (_shakeEffects.HasFlag(effectValue)) ShakeScreen(shake);
    }

    private void PlayProjectileSound(SoundEffect sound)
    {
        SoundPositioner positioner = new SoundPositioner(sound.Lock, transform);
        SoundSystem.Singleton.PlaySFX(new SoundTransporter(sound.Sounds), positioner, sound.Pitch.x, sound.Pitch.y, sound.Volume);
    }

    private void SpawnProjectileEffect(GameObject effect)
    {
        CmdSpawnEffect(NetworkManager.singleton.spawnPrefabs.IndexOf(effect), transform.position);
    }

    private void ShakeScreen(ShakeEffect effect)
    {
        CmdShakeScreen(effect.Duration, effect.Strength);
    }

    #region Network

    [Command(requiresAuthority = false)]
    public void CmdOnRadiusDamage(float radius, float damage, bool explosion)
    {
        TRpcOnRadiusDamage(netIdentity.connectionToClient, radius, damage, explosion);
    }

    [TargetRpc]
    private void TRpcOnRadiusDamage(NetworkConnectionToClient target, float radius, float damage, bool explosion)
    {
        if (!isOwned)
        {
            Debug.LogWarning($"Tried to call OnRadiusDamageInteraction not from projectile owner! projectile {gameObject.name}");
            return;
        }

        OnRadiusDamageInteraction(radius, damage, explosion);
        if (_destroyMode.HasFlag(HitDestroy.OnRadiusDamage) && radius >= _minRadius)
        {
            if (!_requiredToBeExplosion || (_requiredToBeExplosion && explosion))
            {
                _projectileHealth -= damage;
                if (_projectileHealth <= 0) Invoke(nameof(DestroySelf), _delay);
            }
        }
    }

    [Command]
    private void CmdDestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }

    [Command]
    private void CmdSpawnEffect(int regIdx, Vector3 pos)
    {
        GameObject newEffect = Instantiate(NetworkManager.singleton.spawnPrefabs[regIdx], pos, Quaternion.identity);
        NetworkServer.Spawn(newEffect, connectionToClient);
    }

    [Command]
    private void CmdShakeScreen(float duration, float strength)
    {
        SceneGameManager.Singleton.RpcShakeAll(duration, strength);
    }

    #endregion
}

public enum ForceApplyMode
{
    SetForce,
    AddForce
}

[Flags]
public enum HitDestroy
{
    OnPlayer = 1,
    OnMap = 2,
    OnCollide = 4,
    OnTime = 8,
    OnRadiusDamage = 16
}

[Flags]
public enum EffectModes
{
    OnSpawn = 1,
    OnCollide = 2,
    OnDestroy = 4
}

[Serializable]
public class ShakeEffect
{
    public float Duration = 0.2f;
    public float Strength = 0.1f;

    public ShakeEffect(float duration, float strength)
    {
        Duration = duration;
        Strength = strength;
    }
}
