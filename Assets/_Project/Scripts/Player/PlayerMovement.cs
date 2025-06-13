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

        [Header("Settings")]
        public float speed;
        public float gravity;
        public float jumpForce;

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
            inputs = controller.GetInputs();
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
            var dir = (orientation.forward * inputs.move.y + orientation.right * inputs.move.x).normalized;
            currentVelocity = new(dir.x * speed, currentVelocity.y, dir.z * speed);

            if (motor.GroundingStatus.IsStableOnGround) UpdateVelocityOnGround(ref currentVelocity, deltaTime);
            else UpdateVelocityInAir(ref currentVelocity, deltaTime);
        }

        private void UpdateVelocityOnGround(ref Vector3 currentVelocity, float deltaTime)
        {
            if (inputs.wishJumping)
            {
                currentVelocity.y += jumpForce;
                motor.ForceUnground();
            }
        }

        private void UpdateVelocityInAir(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity.y += gravity * deltaTime;
        }
    }
}