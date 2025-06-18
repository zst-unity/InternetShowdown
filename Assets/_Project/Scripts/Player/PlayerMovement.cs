using System;
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
        public float jumpEndDuration;
        public float jumpEndMultiplier;
        public AnimationCurve jumpEndCurve;
        public AnimationCurve jumpEndFalloffCurve;

        private bool _endingJump;
        private float _jumpEndTimer;
        private float _releaseY;
        private float _endJumpHeight;
        private float _jumpEndFalloffValue;

        [Space(9)]
        public float coyoteTime;
        public float bufferTime;

        private float _coyoteTimer;
        private float _bufferTimer;
        private bool _prevWishJumping;

        [Header("Ground Slam")]
        public float groundSlamForce;
        public float groundSlamDistanceForceRatio;

        private bool _groundSlamming;
        private bool _canGroundSlam;

        [Header("Gravity")]
        public float gravity;
        public float gravityClamp;

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

            if (!inputs.wishGroundSlam) _canGroundSlam = true;

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

            var currentY = transform.position.y - _currentJumpHeight;
            if (!inputs.wishJumping)
            {
                if (_jumping)
                {
                    _jumping = false;
                    _endingJump = true;
                    _jumpEndTimer = 0f;
                    _jumpEndFalloffValue = jumpEndFalloffCurve.Evaluate(1f - _jumpTimer / jumpDuration);
                    _endJumpHeight = _currentJumpHeight + (jumpCurve.Evaluate(Mathf.Min(_jumpTimer + jumpEndDuration, jumpDuration) / jumpDuration) * jumpHeight - _currentJumpHeight) * jumpEndMultiplier * _jumpEndFalloffValue;
                    _releaseY = currentY;
                }
            }

            if (_jumping)
            {
                _jumpTimer += deltaTime;
                _currentJumpHeight = jumpCurve.Evaluate(Mathf.Min(_jumpTimer, jumpDuration) / jumpDuration) * jumpHeight;
                motor.MoveCharacter(new(transform.position.x, currentY + _currentJumpHeight, transform.position.z));

                if (_jumpTimer >= jumpDuration) _jumping = false;
            }
            else if (_endingJump)
            {
                motor.MoveCharacter(
                new(
                    transform.position.x,
                    _releaseY + Mathf.Lerp(_currentJumpHeight, _endJumpHeight, jumpEndCurve.Evaluate(_jumpEndTimer / (jumpEndDuration * _jumpEndFalloffValue))),
                    transform.position.z
                ));

                _jumpEndTimer += deltaTime;
                if (_jumpEndTimer > jumpEndDuration * _jumpEndFalloffValue) _endingJump = false;
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
            if (hitNormal.y < 0f)
            {
                _jumping = false;
                _endingJump = false;
            }
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
            if (!_groundSlamming)
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
            }

            if (motor.GroundingStatus.IsStableOnGround) UpdateVelocityOnGround(ref currentVelocity, deltaTime);
            else UpdateVelocityInAir(ref currentVelocity, deltaTime);
        }

        private void UpdateVelocityOnGround(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_groundSlamming) _groundSlamming = false;
        }

        private void UpdateVelocityInAir(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_jumping || _endingJump) currentVelocity.y = 0f;
            else if (currentVelocity.y > gravityClamp)
            {
                currentVelocity.y += gravity * deltaTime;
            }

            if (inputs.wishGroundSlam && !_groundSlamming && _canGroundSlam)
            {
                var wasHit = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1000f, motor.StableGroundLayers);
                if (!wasHit) return;

                currentVelocity.y = Mathf.Lerp(-hit.distance * groundSlamForce, -groundSlamForce, groundSlamDistanceForceRatio);

                _jumping = false;
                _endingJump = false;
                _groundSlamming = true;
                _canGroundSlam = false;
            }
        }
    }
}