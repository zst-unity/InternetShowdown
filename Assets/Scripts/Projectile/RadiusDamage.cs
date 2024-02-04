using Mirror;
using NaughtyAttributes;
using UnityEngine;

public class RadiusDamage : NetworkBehaviour
{
    [SerializeField] private float _radius;
    [SerializeField] private float _damage;

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
            if (obj.TryGetComponent(out NetworkPlayer outPlayer))
            {
                PlayerCurrentStats.Singleton.Damage = _damage;
                outPlayer.CmdHitPlayer(NetworkClient.localPlayer, _damage + PlayerMutationStats.Singleton.Damage);
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
