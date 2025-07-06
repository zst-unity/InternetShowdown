using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    public class AIPlayer : MonoBehaviour, IPlayerController
    {
        public PlayerMovement movement;
        private int _seed;

        private void OnValidate()
        {
            movement = GetComponent<PlayerMovement>();
        }

        private void Awake()
        {
            movement.controller = this;
            _seed = UnityEngine.Random.Range(-10000, 10000);
        }

        private void Update()
        {
        }

        public PlayerInputs GetInputs()
        {
            movement.orientation.rotation = Quaternion.Euler(0f, SamplePerlin(-10000f, 0.1f, 90f), 0f);
            return new()
            {
                move = new(Threshold(SamplePerlin(0f, 0.2f, 1f), 0.2f), Threshold(SamplePerlin(1000f, 0.2f, 1f), 0.2f)),
                wishJumping = SamplePerlin(0f, 0.5f, 1f) > 0.3f,
                wishDashing = UnityEngine.Random.value > 0.9f,
                wishGroundSlam = false,
                orientationX = UnityEngine.Random.Range(-90f, 90f),
            };
        }

        private float SamplePerlin(float offset, float frequency, float amplitude)
        {
            return noise.snoise(new float2(_seed + Time.time + offset, _seed + Time.time + offset) * frequency) * amplitude;
        }

        private float Threshold(float value, float threshold)
        {
            if (value > -threshold && value < threshold) return 0f;
            else return value;
        }
    }
}