using System;
using DG.Tweening;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private const float SHAKE_INCREASE = 0.5f;

    [Header("Sensitivity")]
    [Range(0.1f, 4f)] private float _sensitivityX = 2f;
    [Range(0.1f, 4f)] private float _sensitivityY = 2f;

    [Header("Clamping")]
    [SerializeField] private float _topClamp = -85f;
    [SerializeField] private float _bottomClamp = 90f;

    [Header("Tilting")]
    [SerializeField] private float _tiltSmoothing = 0.15f;
    [SerializeField] private float _tiltAmount = 2.5f;

    [Header("Bobbing")]
    [SerializeField] private float _bobbingAmount = 2.5f;
    [SerializeField] private float _bobbingSpeed = 15.0f;

    [Header("Focus")]
    [SerializeField] private float _fov = 75;
    [SerializeField] private float _fovSmoothing = 0.5f;
    private float _fovDampVelocity = 0.0f;

    [Header("Other")]
    public Transform Orientation;
    public Transform CamHolder;
    public NetworkPlayer Player;
    private Camera _camera;

    private float _rotX;
    private float _rotY;
    private float _rotZ;
    private float _tiltDampVelocity = 0.0f;
    [HideInInspector] public bool BlockMovement;

    private void Start()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        bool isBlocked = BlockMovement || PauseMenu.Singleton.PauseMenuOpened || EverywhereCanvas.Singleton.IsVotingActive || ResultsWindow.Singleton.IsEnabled;

        float mouseX = isBlocked ? 0 : Input.GetAxisRaw("Mouse X") * _sensitivityX;
        float mouseY = isBlocked ? 0 : Input.GetAxisRaw("Mouse Y") * _sensitivityY;

        _rotY += mouseX;
        _rotX -= mouseY;
        _rotX = Math.Clamp(_rotX, _topClamp, _bottomClamp);

        Tilt();

        CamHolder.rotation = Quaternion.Euler(CamHolder.eulerAngles.x, _rotY, CamHolder.eulerAngles.z);
        transform.rotation = Quaternion.Euler(_rotX, _rotY, _rotZ);

        Focus();
        Orientation.localRotation = Quaternion.Euler(transform.localRotation.x, _rotY, transform.localRotation.z);
    }

    private void Tilt()
    {
        float grounded = Player.IsGrounded ? 1.0f : 0.3f;
        _rotZ = Mathf.SmoothDamp
        (
            current: _rotZ,
            target: (Player.GetAxisInputs().x * -_tiltAmount) + (Player.GetAxisInputs().y * (Mathf.Cos(Time.time * _bobbingSpeed) * _bobbingAmount) * grounded),
            currentVelocity: ref _tiltDampVelocity,
            smoothTime: _tiltSmoothing
        );
    }

    private void Focus()
    {
        float targetFov = Player.GetAxisInputs().y > 0 ? _fov + 16.5f : _fov;
        _camera.fieldOfView = Mathf.SmoothDamp(_camera.fieldOfView, targetFov, ref _fovDampVelocity, _fovSmoothing);
    }

    public void Shake(float duration = 0.2f, float strength = 0.25f)
    {
        transform.DOComplete();
        transform.DOShakePosition(duration, strength + SHAKE_INCREASE);
    }

    public void Shake(ShakeEffect effect)
    {
        transform.DOComplete();
        transform.DOShakePosition(effect.Duration, effect.Strength + SHAKE_INCREASE);
    }
}

