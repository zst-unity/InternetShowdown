using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "Player Movement Config", order = 0)]
    public class PlayerMovementConfig : ScriptableObject
    {
        [Header("Movement")]
        public float speed;

        [Space(9)]
        public float groundMoveSmoothingDuration;
        public float airMoveSmoothingDuration;
        public AnimationCurve moveSmootingCurve;

        [Space(9)]
        public AnimationCurve accelerationCurve;
        public float accelerationDuration;

        [Space(9)]
        public AnimationCurve deccelerationCurve;
        public float deccelerationDuration;

        [Header("Jumping")]
        public float jumpHeight;
        public float jumpDuration;
        public AnimationCurve jumpCurve;

        [Space(9)]
        public float jumpEndDuration;
        public float jumpEndMultiplier;
        public AnimationCurve jumpEndCurve;
        public AnimationCurve jumpEndFalloffCurve;

        [Space(9)]
        public float coyoteTime;
        public float bufferTime;

        [Header("Dash")]
        public float dashDistance;
        public float dashDuration;
        public float dashCooldown;
        public float dashBuffer;

        [Header("Ground Slam")]
        public float minGroundSlamForce;
        public float maxGroundSlamForce;
        public float groundSlamForceInterpolationDistance;

        [Header("Wall Running")]
        public LayerMask wallLayers;
        public float wallDetectionDistance;
        public int wallCheckRayCount;

        [Space(9)]
        public float slidingDownSpeed;
        public float wallJumpSmoothing;
        public float wallJumpSpeed;
        public float higherWallDashDirectionThreshold;
        public float lowerWallDashDirectionThreshold;

        [Header("Gravity")]
        public float gravity;
        public float gravityClamp;

        [Header("Other")]
        public float groundAdditionalVelocityDrag;
        public float airAdditionalVelocityDrag;
    }
}