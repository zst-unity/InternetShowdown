using UnityEngine;

public class Mine : ProjectileBase
{
    [SerializeField] private AudioClip _stickSound;
    [SerializeField] private LayerMask _groundLayers;
    private bool _sticked;

    protected override void OnHitMap(Vector3 velocity, ContactPoint contactPoint)
    {
        if (_sticked) return;

        _rb.velocity = Vector3.zero;
        transform.rotation = Quaternion.identity;
        SoundSystem.Singleton.PlaySFX(new SoundTransporter(_stickSound), new SoundPositioner(transform.position), 0.9f, 1.1f);

        Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 5f, _groundLayers);
        transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;

        _sticked = true;
    }
}
