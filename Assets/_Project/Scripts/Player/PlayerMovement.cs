using Game.Player;
using KinematicCharacterController;
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(KinematicCharacterMotor))]
    public class PlayerMovement : MonoBehaviour, ICharacterController
    {
        [Header("Objects")]
        public IPlayerController controller;
        public KinematicCharacterMotor motor;
        public Transform orientation;

        [Header("Movement")]
        public float speed;

        private float _targetSpeed;

        [Space(9)]
        public float moveSmoothingDuration;
        public AnimationCurve moveSmootingCurve;

        private Vector2 _prevMoveInput;
        private Vector2 _fromMoveInput;
        private Vector2 _targetMoveInput;
        private float _elapsedFromMoveInputChange;

        [Space(9)]
        public AnimationCurve accelerationCurve;
        public float accelerationDuration;

        private float _movementTime;

        [Space(9)]
        public AnimationCurve deccelerationCurve;
        public float deccelerationDuration;

        private float _idleTime;

        [Header("Jumping")]
        public float jumpHeight;
        public float jumpDuration;
        public AnimationCurve jumpCurve;

        private bool _jumping;
        private float _jumpTimer;
        private float _currentJumpHeight;

        [Space(9)]
        public float coyoteTime;
        public float bufferTime;

        private float _coyoteTimer;
        private float _bufferTimer;
        private bool _prevWishJumping;

        [Header("Settings")]
        public float gravity;

        private PlayerInputs inputs;

        private void Awake()
        {
            motor.CharacterController = this;
        }

        private void OnValidate()
        {
            motor = GetComponent<KinematicCharacterMotor>();
        }

        public void AfterCharacterUpdate(float deltaTime)
        {

        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            if (controller == null) return;

            _prevWishJumping = inputs.wishJumping;
            inputs = controller.GetInputs();

            if (inputs.wishJumping && !_prevWishJumping)
            {
                _bufferTimer = 0f;
            }
            _bufferTimer += deltaTime;

            if (motor.GroundingStatus.IsStableOnGround) _coyoteTimer = 0f;
            else _coyoteTimer += deltaTime;

            if (_coyoteTimer < coyoteTime)
            {
                _jumpTimer = 0f;
                _currentJumpHeight = 0f;

                if (_bufferTimer <= bufferTime)
                {
                    _coyoteTimer = coyoteTime;
                    motor.ForceUnground();
                    _jumping = true;
                }
            }

            // TODO: прыжок на слопах кал (немног двигает вниз слопа)

            if (!inputs.wishJumping) _jumping = false;

            var currentY = transform.position.y - _currentJumpHeight;
            if (_jumping)
            {
                _jumpTimer += deltaTime;
                _currentJumpHeight = jumpCurve.Evaluate(Mathf.Min(_jumpTimer, jumpDuration) / jumpDuration) * jumpHeight;
                motor.MoveCharacter(new(transform.position.x, currentY + _currentJumpHeight, transform.position.z));

                if (_jumpTimer >= jumpDuration) _jumping = false;
            }
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {

        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {

        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            if (hitNormal.y < 0f) _jumping = false;
        }

        public void PostGroundingUpdate(float deltaTime)
        {

        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {

        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {

        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (inputs.move != _prevMoveInput)
            {
                _fromMoveInput = _targetMoveInput;
                _elapsedFromMoveInputChange = 0f;
            }
            _prevMoveInput = inputs.move;

            if (inputs.move.sqrMagnitude != 0)
            {
                _idleTime = 0f;

                _movementTime += deltaTime;
                _elapsedFromMoveInputChange += Time.deltaTime;

                _targetMoveInput = Vector2.Lerp
                (
                    _fromMoveInput,
                    inputs.move.normalized,
                    moveSmootingCurve.Evaluate(Mathf.Min(_elapsedFromMoveInputChange, moveSmoothingDuration) / moveSmoothingDuration)
                );

                _targetSpeed = speed * accelerationCurve.Evaluate(Mathf.Min(_movementTime, accelerationDuration) / accelerationDuration);
            }
            else
            {
                _movementTime = 0f;

                _idleTime += deltaTime;
                _targetMoveInput = Vector2.Lerp
                (
                    _fromMoveInput,
                    Vector2.zero,
                    deccelerationCurve.Evaluate(Mathf.Min(_idleTime, deccelerationDuration) / deccelerationDuration)
                );
            }

            var dir = orientation.forward * _targetMoveInput.y + orientation.right * _targetMoveInput.x;
            currentVelocity = new(dir.x * _targetSpeed, currentVelocity.y, dir.z * _targetSpeed);

            if (motor.GroundingStatus.IsStableOnGround) UpdateVelocityOnGround(ref currentVelocity, deltaTime);
            else UpdateVelocityInAir(ref currentVelocity, deltaTime);
        }

        private void UpdateVelocityOnGround(ref Vector3 currentVelocity, float deltaTime)
        {
        }

        private void UpdateVelocityInAir(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_jumping) currentVelocity.y = 0f;
            else currentVelocity.y += gravity * deltaTime;
        }
    }
}