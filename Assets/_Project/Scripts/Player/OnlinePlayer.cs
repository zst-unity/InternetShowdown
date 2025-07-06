using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    public class OnlinePlayer : MonoBehaviour, IPlayerController
    {
        public PlayerMovement movement;

        [Header("Speedlines")]
        public Transform speedlines;
        public Material speedlinesFullscreenMaterial;
        public float minSpeedlinesSpeed;
        public float maxSpeedlinesSpeed;
        public AnimationCurve speedlinesAlphaCurve;
        public float speedlinesAlphaSmoothingSpeed;

        private Camera _camera;
        private float _cameraRotX;
        private Vector3 _prevPosition;
        private float _targetSpeedlinesAlpha;

        private void OnValidate()
        {
            movement = GetComponent<PlayerMovement>();
        }

        private void Awake()
        {
            movement.controller = this;
            Cursor.lockState = CursorLockMode.Locked;
            _camera = Camera.main;
        }

        private void Update()
        {
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

            var dir = transform.position - _prevPosition;
            var speed = dir.magnitude / Time.deltaTime;

            if (speed >= minSpeedlinesSpeed)
            {
                dir.Normalize();

                var alpha = speedlinesAlphaCurve.Evaluate((speed - minSpeedlinesSpeed) / maxSpeedlinesSpeed);
                _targetSpeedlinesAlpha = Mathf.Lerp(_targetSpeedlinesAlpha, alpha, Time.deltaTime * speedlinesAlphaSmoothingSpeed);

                speedlines.transform.SetPositionAndRotation(_camera.transform.position + dir * 2.3f, Quaternion.LookRotation(-dir));
            }
            else _targetSpeedlinesAlpha = Mathf.Lerp(_targetSpeedlinesAlpha, 0f, Time.deltaTime * speedlinesAlphaSmoothingSpeed);

            speedlinesFullscreenMaterial.SetFloat("_alpha", _targetSpeedlinesAlpha);
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