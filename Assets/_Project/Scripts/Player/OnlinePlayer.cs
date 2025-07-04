using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    public class OnlinePlayer : MonoBehaviour, IPlayerController
    {
        public PlayerMovement movement;

        private Camera _camera;
        private float _cameraRotX;

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