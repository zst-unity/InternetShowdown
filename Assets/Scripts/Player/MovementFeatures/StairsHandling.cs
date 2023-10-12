using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StairsHandling : NetworkBehaviour
{
    [Header("Objects")]
    [SerializeField] private Transform _orientation;
    [SerializeField] private Rigidbody _rb;

    [Header("Settings")]
    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private float _minStepHeight = 0.05f;
    [SerializeField] private float _maxStepHeight = 0.3f;

    [Space(9)]

    [SerializeField] private float _stepSpeed = 0.1f;

    private Vector3 _stepMinOrigin
    {
        get => transform.position + Vector3.up * _minStepHeight;
    }

    private Vector3 _stepMaxOrigin
    {
        get => transform.position + Vector3.up * _maxStepHeight;
    }

    private void OnValidate()
    {
        TryGetComponent(out _rb);
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        bool IsMinStep = Physics.Raycast(_stepMinOrigin, _orientation.forward, 1.25f, _targetLayers);
        bool IsMaxStep = Physics.Raycast(_stepMaxOrigin, _orientation.forward, 1.25f, _targetLayers);

        if (IsMinStep && !IsMaxStep)
        {
            _rb.velocity = new Vector3(_rb.velocity.x, _stepSpeed, _rb.velocity.z);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = ColorISH.Green;
        Gizmos.DrawLine(_stepMinOrigin, _stepMinOrigin + _orientation.forward * 1.25f);

        Gizmos.color = ColorISH.Red;
        Gizmos.DrawLine(_stepMaxOrigin, _stepMaxOrigin + _orientation.forward * 1.25f);
    }
}
