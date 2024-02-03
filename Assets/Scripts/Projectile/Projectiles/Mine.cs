using UnityEngine;

public class Mine : ProjectileBase
{
    protected override void OnHitMap(Vector3 velocity, ContactPoint contactPoint)
    {
        _rb.velocity = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }
}
