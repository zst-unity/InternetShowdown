using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] private float _force;
    [SerializeField] private AudioClip _sound;

    private void OnCollisionStay(Collision other)
    {
        if (other.gameObject.TryGetComponent(out Rigidbody hit))
        {
            hit.velocity = new
            (
                x: hit.velocity.x + (transform.up.x * _force),
                y: transform.up.y * _force,
                z: hit.velocity.z + (transform.up.z * _force)
            );

            SoundSystem.PlaySound(new SoundTransporter(_sound), new SoundPositioner(transform.position), SoundType.SFX, volume: 0.45f, pitchMin: 0.95f, pitchMax: 1.1f);
        }
    }
}
