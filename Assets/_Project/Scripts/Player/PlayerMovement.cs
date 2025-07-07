using System;
using KinematicCharacterController;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Player
{
    [RequireComponent(typeof(KinematicCharacterMotor))]
    public class PlayerMovement : NetworkBehaviour, ICharacterController
    {
        public PlayerMovementConfig config;
        public IPlayerController controller;
        public KinematicCharacterMotor motor;
        public Transform orientation;

        // movement
        private float _targetSpeed;
        private Vector2 _prevMoveInput;
        private Vector2 _fromMoveInput;
        private Vector2 _targetMoveInput;
        private float _elapsedFromMoveInputChange;
        private float _movementTime;
        private float _idleTime;

        // jumping
        private bool _jumping;
        private float _jumpTimer;
        private float _currentJumpHeight;
        private bool _endingJump;
        private float _jumpEndTimer;
        private float _releaseY;
        private float _endJumpHeight;
        private float _jumpEndFalloffValue;
        private float _coyoteTimer;
        private float _bufferTimer;
        private bool _prevWishJumping;

        // dash
        private bool _dashing;
        private bool _canDash;
        private float _dashTimer;
        private Vector3 _dashStartPos;
        private Vector3 _dashDirection;
        private float _dashCooldownTimer;
        private float _dashBufferTimer;
        private bool _prevWishDashing;

        // ground slam
        private bool _groundSlamming;
        private float _groundSlamForce;
        private bool _canGroundSlam;

        // wall running
        private bool _walled;
        private bool _prevWalled;
        private RaycastHit _wallHitInfo;
        private bool _jumpingFromGround;

        // other
        public PlayerInputs inputs;
        private Vector3 _additionalVelocity;
        private float _gravityVelocity;

        public UnityEvent onGroundSlamLanded = new();

        private void Awake()
        {
            motor.enabled = false;
        }

        public void EnableMotor()
        {
            motor.enabled = true;
            motor.CharacterController = this;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            motor = GetComponent<KinematicCharacterMotor>();
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            var drag = motor.GroundingStatus.IsStableOnGround ? config.groundAdditionalVelocityDrag : config.airAdditionalVelocityDrag;
            _additionalVelocity *= 1f - drag * deltaTime;

            if (new Vector2(_additionalVelocity.x, _additionalVelocity.z).magnitude <= 0.5f)
                _additionalVelocity = Vector3.up * _additionalVelocity.y;
        }

        private void CheckWalled()
        {
            var hit = new RaycastHit();
            _prevWalled = _walled;

            _walled = false;
            if (!motor.GroundingStatus.IsStableOnGround && !_groundSlamming && !_jumpingFromGround)
            {
                var origin = transform.position + Vector3.up * motor.Capsule.height / 2f;
                var maxdist = config.wallDetectionDistance + motor.Capsule.radius;
                for (int i = 0; i < config.wallCheckRayCount; i++)
                {
                    var x = i * Mathf.PI * 2 / config.wallCheckRayCount;
                    var dir = new Vector3(Mathf.Sin(x), 0f, Mathf.Cos(x));
                    if (Physics.Raycast(origin, orientation.rotation * dir, out hit, maxdist, config.wallLayers))
                    {
                        _walled = true;
                        break;
                    }
                }
            }

            if (_walled) _wallHitInfo = hit;

            if (!_prevWalled && _walled)
            {
                _jumping = false;
                _endingJump = false;
                _dashing = false;
            }
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            if (controller == null) return;
            CheckWalled();

            _prevWishJumping = inputs.wishJumping;
            _prevWishDashing = inputs.wishDashing;
            inputs = controller.GetInputs();

            if (!inputs.wishGroundSlam) _canGroundSlam = true;
            if (!motor.GroundingStatus.IsStableOnGround
                && inputs.wishGroundSlam
                && !_groundSlamming
                && _canGroundSlam
                && Physics.Raycast(transform.position, Vector3.down, out var hitInfo, 1000f, motor.StableGroundLayers))
            {
                _jumping = false;
                _endingJump = false;
                _dashing = false;
                _groundSlamForce = Mathf.Lerp(config.minGroundSlamForce, config.maxGroundSlamForce, hitInfo.distance / config.groundSlamForceInterpolationDistance);

                _groundSlamming = true;
                _canGroundSlam = false;
                _bufferTimer = config.bufferTime;
            }

            if (inputs.wishDashing && !_prevWishDashing)
            {
                _dashBufferTimer = 0f;
            }
            _dashBufferTimer += deltaTime;

            if (_dashBufferTimer < config.dashBuffer && !_dashing && _canDash && _dashCooldownTimer <= 0f)
            {
                _dashing = true;
                _canDash = false;
                _dashTimer = 0f;
                _dashStartPos = transform.position;

                var playerViewRot = Quaternion.Euler(new(inputs.orientationX, orientation.eulerAngles.y, 0f));
                if (_walled)
                {
                    var playerViewDir = playerViewRot * Vector3.forward;
                    var playerViewDirMasked = new Vector3(playerViewDir.x, 0f, playerViewDir.z);
                    if (Vector3.Dot(playerViewDirMasked, _wallHitInfo.normal) > config.higherWallDashDirectionThreshold)
                        _dashDirection = playerViewDir;
                    else if (Vector3.Dot(playerViewDirMasked, _wallHitInfo.normal) < config.lowerWallDashDirectionThreshold)
                        _dashDirection = -playerViewDir;
                    else _dashDirection = _wallHitInfo.normal;
                }
                else
                {
                    var relative = inputs.move.sqrMagnitude == 0 ? Vector3.forward : new Vector3(inputs.move.x, 0f, inputs.move.y);
                    _dashDirection = playerViewRot * relative;
                }

                _jumping = false;
                _endingJump = false;
                _groundSlamming = false;
                motor.ForceUnground(config.dashDuration);

                _dashBufferTimer = config.dashBuffer;
            }

            if (inputs.wishJumping && !_prevWishJumping)
            {
                _bufferTimer = 0f;
            }
            _bufferTimer += deltaTime;

            if (_dashing)
            {
                _coyoteTimer = 0f;
                _dashTimer += deltaTime;
                motor.MoveCharacter(Vector3.Lerp(_dashStartPos, _dashStartPos + _dashDirection * config.dashDistance, _dashTimer / config.dashDuration));

                _dashCooldownTimer = config.dashCooldown;
                return;
            }

            _dashCooldownTimer -= deltaTime;

            if (motor.GroundingStatus.IsStableOnGround || _dashing || _walled)
            {
                _coyoteTimer = 0f;
                if (!inputs.wishDashing) _canDash = true;
            }
            else _coyoteTimer += deltaTime;

            if (_coyoteTimer < config.coyoteTime)
            {
                _jumpTimer = 0f;
                _currentJumpHeight = 0f;

                if (_bufferTimer <= config.bufferTime) BeginJump();
            }

            var currentY = transform.position.y - _currentJumpHeight;
            if (!inputs.wishJumping)
            {
                if (_jumping)
                {
                    _jumping = false;
                    _endingJump = true;
                    _jumpEndTimer = 0f;
                    _jumpEndFalloffValue = config.jumpEndFalloffCurve.Evaluate(1f - _jumpTimer / config.jumpDuration);
                    _endJumpHeight = _currentJumpHeight + (config.jumpCurve.Evaluate(Mathf.Min(_jumpTimer + config.jumpEndDuration, config.jumpDuration) / config.jumpDuration) * config.jumpHeight - _currentJumpHeight) * config.jumpEndMultiplier * _jumpEndFalloffValue;
                    _releaseY = currentY;
                }
            }

            if (_jumping)
            {
                _jumpTimer += deltaTime;
                _currentJumpHeight = config.jumpCurve.Evaluate(Mathf.Min(_jumpTimer, config.jumpDuration) / config.jumpDuration) * config.jumpHeight;
                motor.MoveCharacter(new(transform.position.x, currentY + _currentJumpHeight, transform.position.z));

                if (_jumpTimer >= config.jumpDuration) _jumping = false;
            }
            else
            {
                _jumpingFromGround = false;
                if (_endingJump)
                {
                    motor.MoveCharacter(
                    new(
                        transform.position.x,
                        _releaseY + Mathf.Lerp(_currentJumpHeight, _endJumpHeight, config.jumpEndCurve.Evaluate(_jumpEndTimer / (config.jumpEndDuration * _jumpEndFalloffValue))),
                        transform.position.z
                    ));

                    _jumpEndTimer += deltaTime;
                    if (_jumpEndTimer > config.jumpEndDuration * _jumpEndFalloffValue) _endingJump = false;
                }
            }
        }

        private void BeginJump()
        {
            _coyoteTimer = config.coyoteTime;
            motor.ForceUnground();
            _jumping = true;
            if (!_walled) _jumpingFromGround = true;
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
            if (Vector3.Dot(hitNormal, _dashDirection) < -0.9f) _dashing = false;
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
            if (_dashing)
            {
                if ((_dashTimer >= config.dashDuration) || (inputs.wishJumping && !_prevWishJumping))
                {
                    _dashing = false;
                    _additionalVelocity = _dashDirection * (config.dashDistance / config.dashDuration);
                }
            }

            if (_walled && _jumping)
            {
                _additionalVelocity = _wallHitInfo.normal * config.wallJumpSpeed;
            }

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

                var smoothingDuration = motor.GroundingStatus.IsStableOnGround ? config.groundMoveSmoothingDuration : config.airMoveSmoothingDuration;
                _targetMoveInput = Vector2.Lerp
                (
                    _fromMoveInput,
                    inputs.move.normalized,
                    config.moveSmootingCurve.Evaluate(Mathf.Min(_elapsedFromMoveInputChange, smoothingDuration) / smoothingDuration)
                );

                _targetSpeed = config.speed * config.accelerationCurve.Evaluate(Mathf.Min(_movementTime, config.accelerationDuration) / config.accelerationDuration);
            }
            else
            {
                _movementTime = 0f;

                _idleTime += deltaTime;
                _targetMoveInput = Vector2.Lerp
                (
                    _fromMoveInput,
                    Vector2.zero,
                    config.deccelerationCurve.Evaluate(Mathf.Min(_idleTime, config.deccelerationDuration) / config.deccelerationDuration)
                );
            }
            var dir = orientation.rotation * new Vector3(_targetMoveInput.x, 0f, _targetMoveInput.y);

            var movementVelocity = dir * _targetSpeed;

            if (Vector3.Dot(new Vector3(_additionalVelocity.x, 0f, _additionalVelocity.z), dir) < 0f)
            {
                _additionalVelocity.x += dir.x;
                _additionalVelocity.z += dir.z;
            }

            currentVelocity = movementVelocity + _additionalVelocity;

            var addvel = _jumping ? new Vector3(_additionalVelocity.x, 0f, _additionalVelocity.z) : _additionalVelocity;

            if (_groundSlamming) currentVelocity = new(currentVelocity.x, _groundSlamForce, currentVelocity.z);
            else if (_walled) currentVelocity = new Vector3(movementVelocity.x, -config.slidingDownSpeed, movementVelocity.z) + addvel;
            else currentVelocity = movementVelocity + addvel + Vector3.up * _gravityVelocity;

            if (motor.GroundingStatus.IsStableOnGround) UpdateVelocityOnGround(ref currentVelocity, deltaTime);
            else UpdateVelocityInAir(ref currentVelocity, deltaTime);
        }

        private void UpdateVelocityOnGround(ref Vector3 currentVelocity, float deltaTime)
        {
            _gravityVelocity = 0f;
            if (_groundSlamming)
            {
                _groundSlamming = false;
                onGroundSlamLanded?.Invoke();
            }
        }

        private void UpdateVelocityInAir(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_jumping || _endingJump || _dashing || _walled) _gravityVelocity = 0f;
            else if (currentVelocity.y > config.gravityClamp)
            {
                _gravityVelocity += config.gravity * deltaTime;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            var origin = transform.position + Vector3.up * motor.Capsule.height / 2f;
            var distance = config.wallDetectionDistance + motor.Capsule.radius;
            for (int i = 0; i < config.wallCheckRayCount; i++)
            {
                var x = i * Mathf.PI * 2 / config.wallCheckRayCount;
                Gizmos.DrawRay(origin, new Vector3(Mathf.Sin(x), 0f, Mathf.Cos(x)) * distance);
            }
        }
    }
}