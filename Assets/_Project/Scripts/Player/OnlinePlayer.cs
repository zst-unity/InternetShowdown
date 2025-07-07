using System.Collections.Generic;
using System.Linq;
using Mirror;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    public class OnlinePlayer : NetworkBehaviour, IPlayerController
    {
        public GameObject cameraPrefab;
        public GameObject model;
        public PlayerMovement movement;
        public Transform cameraHolder;
        public int speedRecordSize;

        private Queue<float> _speedRecord;

        [Header("Speedlines")]
        public bool enableSpeedlines;
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

        [Header("Side Run Tilt")]
        public float maxSideRunTilt;
        public float sideRunTiltSmoothingSpeed;

        private float _sideRunTilt;

        [Header("Camera Bop")]
        public float cameraBopAmplitude;
        public float cameraBopTiltAmplitude;
        public float cameraBopFrequency;
        public float cameraBopStopSpeed;

        private float _cameraBopHeight;
        private float _cameraBopTilt;

        [Header("Camera Shake")]
        public float cameraShakeFrequency;
        public float cameraShakeFalloffSpeed;

        [Space(9)]
        public float groundSlamCameraShakeMultiplier;
        public float maxGroundSlamCameraShake;

        private PlayerCamera _camera;
        private float _cameraRotX;
        private Vector3 _prevPosition;
        private float _timeSinceRunning;

        private Vector3 _cameraShake;
        private float _cameraShakeMult;

        protected override void OnValidate()
        {
            base.OnValidate();
            movement = GetComponent<PlayerMovement>();
        }

        public override void OnStartLocalPlayer()
        {
            _speedRecord = new(speedRecordSize);

            Cursor.lockState = CursorLockMode.Locked;
            _camera = Instantiate(cameraPrefab, cameraHolder).GetComponent<PlayerCamera>();
            model.SetActive(false);

            movement.controller = this;
            movement.EnableMotor();
            movement.onGroundSlamLanded.AddListener((dist) => ShakeCamera(Mathf.Min(dist * groundSlamCameraShakeMultiplier, maxGroundSlamCameraShake)));
        }

        public void ShakeCamera(float amplitude)
        {
            _cameraShakeMult = amplitude;
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            // CAMERA SHAKE
            var x = Time.time * cameraShakeFrequency;
            _cameraShake = new Vector3
            (
                noise.snoise(new float2(x)),
                noise.snoise(new float2(x + 1000f)),
                noise.snoise(new float2(x - 1000f))
            ) * _cameraShakeMult;
            _cameraShakeMult = Mathf.Lerp(_cameraShakeMult, 0f, Time.deltaTime * cameraShakeFalloffSpeed);

            // SIDE RUN TILT
            var targetSideRunTilt = movement.inputs.move.normalized.x * maxSideRunTilt;
            _sideRunTilt = Mathf.Lerp(_sideRunTilt, targetSideRunTilt, Time.deltaTime * sideRunTiltSmoothingSpeed);

            // CAMERA BOP
            if (movement.inputs.move.sqrMagnitude > 0 && movement.motor.GroundingStatus.IsStableOnGround) _timeSinceRunning += Time.deltaTime;
            else _timeSinceRunning = 0f;

            if (_timeSinceRunning == 0f)
            {
                _cameraBopHeight = Mathf.Lerp(_cameraBopHeight, 0f, Time.deltaTime * cameraBopStopSpeed);
                _cameraBopTilt = Mathf.Lerp(_cameraBopTilt, 0f, Time.deltaTime * cameraBopStopSpeed);
            }
            else
            {
                _cameraBopHeight = Mathf.Max(Mathf.Sin(_timeSinceRunning * cameraBopFrequency), Mathf.Sin(_timeSinceRunning * cameraBopFrequency + Mathf.PI)) * cameraBopAmplitude;
                _cameraBopTilt = Mathf.Sin(_timeSinceRunning * cameraBopFrequency) * cameraBopTiltAmplitude;
            }

            _camera.transform.localPosition = _cameraShake + Vector3.up * _cameraBopHeight;

            // CAMERA ROTATION
            var delta = Input.mousePositionDelta * 0.2f;
            movement.orientation.localEulerAngles += new Vector3(0f, delta.x, 0f);
            _cameraRotX -= delta.y;
            _cameraRotX = Mathf.Clamp(_cameraRotX, -90f, 90f);
            _camera.transform.localRotation = Quaternion.Euler
            (
                _cameraRotX,
                0f,
                _sideRunTilt + _cameraBopTilt
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
                _camera.camera.fieldOfView = Mathf.Lerp(_camera.camera.fieldOfView, targetFov, Time.deltaTime * FOVSmoothingSpeed);
            }
            else _camera.camera.fieldOfView = idleFOV;

            // SPEEDLINES
            if (enableSpeedlines)
            {
                if (speed >= minSpeedlinesSpeed)
                {
                    var targetAlpha = speedlinesAlphaCurve.Evaluate((speed - minSpeedlinesSpeed) / maxSpeedlinesSpeed);
                    _currentSpeedlinesAlpha = Mathf.Lerp(_currentSpeedlinesAlpha, targetAlpha, Time.deltaTime * speedlinesAlphaSmoothingSpeed);

                    _camera.speedlines.transform.SetPositionAndRotation(_camera.transform.position + dir * 2.3f, Quaternion.LookRotation(-dir));
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