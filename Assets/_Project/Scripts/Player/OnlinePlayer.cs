using System.Collections.Generic;
using System.Linq;
using Game.Inputs;
using Game.Network.Messages;
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

        [Space(9)]
        public int speedRecordSize;

        private Queue<float> _speedRecord;

        [Header("Sounds")]
        public AudioSource leftFootstepSource;
        public AudioSource rightFootstepSource;
        public AudioSource wallLockSource;

        private float _footstepTimer;

        [Space(9)]
        public AudioSource jumpSource;
        public float jumpPitchUpRate;
        public float jumpPitchDownRate;
        public AnimationCurve jumpVolumeCurve;
        public float jumpVolumeMultiplier;

        private float _jumpVolumeTimer;

        [Space(9)]
        public AudioSource windAudioSource;
        public float windVolumeSmoothingSpeed;
        public float windVolumeMultiplier;

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
        private float _mouseSens;

        private PlayerActions _actions;

        protected override void OnValidate()
        {
            base.OnValidate();
            movement = GetComponent<PlayerMovement>();
        }

        private void Start()
        {
            windAudioSource.volume = 0f;
            jumpSource.volume = 0f;
        }

        public override void OnStartLocalPlayer()
        {
            _speedRecord = new(speedRecordSize);

            Cursor.lockState = CursorLockMode.Locked;
            _camera = Instantiate(cameraPrefab, cameraHolder).GetComponent<PlayerCamera>();
            model.SetActive(false);
            _mouseSens = PlayerPrefs.GetFloat("sens");

            _actions = new();

            movement.onGroundSlamLanded.AddListener((dist) =>
            {
                ShakeCamera(Mathf.Min(dist * groundSlamCameraShakeMultiplier, maxGroundSlamCameraShake));
            });

            movement.onJump.AddListener(() =>
            {
                jumpSource.pitch = 0.6f;
                _jumpVolumeTimer = movement.config.jumpDuration;
                jumpSource.Play();
            });

            movement.onWalled.AddListener((_) => wallLockSource.Play());
            movement.onCollide.AddListener((collider) =>
            {
                if (!collider.CompareTag("Portal")) return;
                NetworkClient.Send<ClientRequestMapLoad>(new());
            });

            movement.controller = this;
            _actions.Enable();
            movement.EnableMotor();
        }

        public void ShakeCamera(float amplitude)
        {
            _cameraShakeMult = amplitude;
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (Cursor.lockState == CursorLockMode.Locked) Cursor.lockState = CursorLockMode.None;
                else Cursor.lockState = CursorLockMode.Locked;
            }

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
            if (movement.inputs.move.sqrMagnitude > 0 && movement.motor.GroundingStatus.IsStableOnGround)
            {
                _timeSinceRunning += Time.deltaTime;
                _footstepTimer += Time.deltaTime;
            }
            else
            {
                _timeSinceRunning = 0f;
                _footstepTimer = 0f;
            }

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

            // FOOTSTEPS
            if (_footstepTimer >= Mathf.PI / cameraBopFrequency)
            {
                if (_cameraBopTilt < 0f) leftFootstepSource.Play();
                else rightFootstepSource.Play();
                _footstepTimer = 0f;
            }

            // CAMERA ROTATION
            var delta = _actions.Camera.Look.ReadValue<Vector2>() * _mouseSens;
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
            var velocity = transform.position - _prevPosition; ;
            var rawSpeed = velocity.magnitude / Time.deltaTime;
            var dir = velocity.normalized;

            if (_speedRecord.Count == speedRecordSize) _speedRecord.Dequeue();
            _speedRecord.Enqueue(rawSpeed);
            var speed = _speedRecord.ToArray().Average();

            // WIND SOUND
            windAudioSource.volume = Mathf.Lerp
            (
                windAudioSource.volume,
                speedlinesAlphaCurve.Evaluate((speed - minSpeedlinesSpeed) / maxSpeedlinesSpeed) * windVolumeMultiplier,
                Time.deltaTime * windVolumeSmoothingSpeed
            );

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

            // JUMP SOUND
            var pitchDir = Mathf.Round((transform.position.y - _prevPosition.y) * 20f) / 20f;
            var pitchFactor = pitchDir > 0 ? jumpPitchUpRate : pitchDir == 0 ? 0f : -jumpPitchDownRate;
            jumpSource.pitch += pitchFactor * (1f - _jumpVolumeTimer / movement.config.jumpDuration);
            jumpSource.volume = jumpVolumeCurve.Evaluate(1f - _jumpVolumeTimer / movement.config.jumpDuration) * jumpVolumeMultiplier;
            _jumpVolumeTimer -= Time.deltaTime;

            _prevPosition = transform.position;
        }

        public PlayerInputs GetInputs()
        {
            return new()
            {
                move = _actions.Movement.Move.ReadValue<Vector2>(),
                wishJumping = _actions.Movement.Jump.inProgress,
                wishDashing = _actions.Movement.Dash.inProgress,
                wishGroundSlam = _actions.Movement.GroundSlam.inProgress,
                orientationX = _cameraRotX,
            };
        }
    }
}