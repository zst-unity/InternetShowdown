using UnityEngine;

namespace Game.Player
{
    public struct PlayerInputs
    {
        public Vector2 move;
        public bool wishJumping;
        public bool wishDashing;
        public bool wishGroundSlam;
        public float orientationX;
    }
}