using Mirror;
using NaughtyAttributes;
using UnityEngine;

public class RadiusDamage : NetworkBehaviour
{
    [SerializeField] private float _radius;
    [SerializeField] private float _damage;
    [SerializeField] private bool _explosion;
    [SerializeField] private float _knockback;

    [Space(9)]
    [SerializeField] private bool _castDamageOvertime;
    [SerializeField, ShowIf(nameof(_castDamageOvertime)), AllowNesting] private float _lifetime = 5f;
    [SerializeField, ShowIf(nameof(_castDamageOvertime)), AllowNesting] private float _interval = 0.5f;

    public override void OnStartAuthority()
    {
        if (_castDamageOvertime)
        {
            InvokeRepeating(nameof(CastRadiusDamage), 0, _interval);
            Invoke(nameof(CancelDamageOvertime), _lifetime);

            return;
        };

        CastRadiusDamage();
    }

    private void CastRadiusDamage()
    {
        Collider[] all = Physics.OverlapSphere(transform.position, _radius);

        foreach (Collider obj in all)
        {
            if (obj.TryGetComponent(out NetworkPlayer player))
            {
                player.CmdHitPlayer(NetworkClient.localPlayer, NetworkPlayer.MutationStats.Mutate(_damage, NetworkPlayer.MutationStats.damage));
                if (_knockback != 0f) player.CmdKnockback(_knockback, transform.position, _radius, 1.5f);
            }
            else if (obj.TryGetComponent(out ProjectileBase projectile))
            {
                projectile.CmdOnRadiusDamage(_radius, _damage, _explosion);
            }
        }
    }

    private void CancelDamageOvertime()
    {
        CancelInvoke(nameof(CastRadiusDamage));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = ColorISH.Yellow;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
