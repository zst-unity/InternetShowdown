using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] private float _force;
    [SerializeField] private AudioClip _sound;

    private void OnTriggerStay(Collider other)
    {
        if (other.attachedRigidbody)
        {
            other.attachedRigidbody.velocity = new
            (
                x: other.attachedRigidbody.velocity.x + (transform.up.x * _force),
                y: transform.up.y * _force,
                z: other.attachedRigidbody.velocity.z + (transform.up.z * _force)
            );

            SoundSystem.PlaySound(new SoundTransporter(_sound), new SoundPositioner(transform.position), SoundType.SFX, volume: 0.45f, pitchMin: 0.95f, pitchMax: 1.1f);
        }
    }
}
