using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    public class OnlinePlayer : MonoBehaviour, IPlayerController
    {
        public PlayerMovement movement;
        public int speedRecordSize;

        private Queue<float> _speedRecord;

        [Header("Speedlines")]
        public bool enableSpeedlines;
        public Transform speedlines;
        public Material speedlinesFullscreenMaterial;
        public float minSpeedlinesSpeed;
        public float maxSpeedlinesSpeed;
        public AnimationCurve speedlinesAlphaCurve;
        public float speedlinesAlphaSmoothingSpeed;

        private float _currentSpeedlinesAlpha;

        [Header("Speed Affects FOV")]
        public bool enableSpeedAffectsFOV;
        public float idleFOV;
        public float maxFOV;
        public float maxFOVSpeed;
        public AnimationCurve FOVCurve;
        public float FOVSmoothingSpeed;

        private Camera _camera;
        private float _cameraRotX;
        private Vector3 _prevPosition;

        private void OnValidate()
        {
            movement = GetComponent<PlayerMovement>();
        }

        private void Awake()
        {
            _speedRecord = new(speedRecordSize);
            movement.controller = this;
            Cursor.lockState = CursorLockMode.Locked;
            _camera = Camera.main;
        }

        private void Update()
        {
            // CAMERA ROTATION
            var delta = Input.mousePositionDelta * 0.2f;
            movement.orientation.localEulerAngles += new Vector3(0f, delta.x, 0f);
            _cameraRotX -= delta.y;
            _cameraRotX = Mathf.Clamp(_cameraRotX, -90f, 90f);
            _camera.transform.localRotation = Quaternion.Euler
            (
                _cameraRotX,
                0f,
                0f
            );

            // FIND SPEED
            var vel = transform.position - _prevPosition;
            var rawSpeed = vel.magnitude / Time.deltaTime;
            var dir = vel.normalized;

            if (_speedRecord.Count == speedRecordSize) _speedRecord.Dequeue();
            _speedRecord.Enqueue(rawSpeed);
            var speed = _speedRecord.ToArray().Average();

            // FOV
            if (enableSpeedAffectsFOV)
            {
                var dot = Vector3.Dot(_camera.transform.forward, dir);
                var targetFov = Mathf.Lerp(idleFOV, maxFOV, FOVCurve.Evaluate(speed / maxFOVSpeed * Mathf.Abs(dot)));
                _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFov, Time.deltaTime * FOVSmoothingSpeed);
            }
            else _camera.fieldOfView = idleFOV;

            // SPEEDLINES
            if (enableSpeedlines)
            {
                if (speed >= minSpeedlinesSpeed)
                {
                    var targetAlpha = speedlinesAlphaCurve.Evaluate((speed - minSpeedlinesSpeed) / maxSpeedlinesSpeed);
                    _currentSpeedlinesAlpha = Mathf.Lerp(_currentSpeedlinesAlpha, targetAlpha, Time.deltaTime * speedlinesAlphaSmoothingSpeed);

                    speedlines.transform.SetPositionAndRotation(_camera.transform.position + dir * 2.3f, Quaternion.LookRotation(-dir));
                }
                else _currentSpeedlinesAlpha = Mathf.Lerp(_currentSpeedlinesAlpha, 0f, Time.deltaTime * speedlinesAlphaSmoothingSpeed);

                speedlinesFullscreenMaterial.SetFloat("_alpha", _currentSpeedlinesAlpha);
            }
            else speedlinesFullscreenMaterial.SetFloat("_alpha", 0f);

            _prevPosition = transform.position;
        }

        public PlayerInputs GetInputs()
        {
            return new()
            {
                move = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                wishJumping = Input.GetKey(KeyCode.Space),
                wishDashing = Input.GetKey(KeyCode.LeftShift),
                wishGroundSlam = Input.GetKey(KeyCode.LeftControl),
                orientationX = _cameraRotX,
            };
        }
    }
}