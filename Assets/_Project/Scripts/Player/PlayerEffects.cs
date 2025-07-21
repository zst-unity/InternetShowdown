using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Game.Player
{
    // i want to die
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerEffects : NetworkBehaviour
    {
        public bool serverSide;
        public PlayerEffectsConfig config;
        public Transform audioSourcesContainer;
        public PlayerMovement movement;

        private bool _wasOnGround;

        private Vector3 _velocity;
        private Vector3 _prevPosition;
        private Queue<Vector3> _velocityRecord;

        private AudioSource _jumpPrimaryAudioSource;
        private AudioSource _dashAudioSource;
        private AudioSource _groundSlamAudioSource;
        private AudioSource _landAudioSource;
        private AudioSource _wallSlideAudioSource;
        private AudioSource _skidAudioSource;

        private bool Owned => serverSide ? isServer : isOwned;
        [SyncVar] private bool _walled;
        [SyncVar] private Vector3 _wallNormal;

        protected override void OnValidate()
        {
            base.OnValidate();
            movement = GetComponent<PlayerMovement>();
        }

        private void Awake()
        {
            _jumpPrimaryAudioSource = Instantiate(config.jumpPrimaryAudioSource, audioSourcesContainer).GetComponent<AudioSource>();
            _dashAudioSource = Instantiate(config.dashAudioSource, audioSourcesContainer).GetComponent<AudioSource>();
            _groundSlamAudioSource = Instantiate(config.groundSlamAudioSource, audioSourcesContainer).GetComponent<AudioSource>();
            _landAudioSource = Instantiate(config.landAudioSource, audioSourcesContainer).GetComponent<AudioSource>();
            _wallSlideAudioSource = Instantiate(config.wallSlideAudioSource, audioSourcesContainer).GetComponent<AudioSource>();
            _skidAudioSource = Instantiate(config.skidAudioSource, audioSourcesContainer).GetComponent<AudioSource>();

            _wallSlideAudioSource.volume = 0f;
        }

        private void Start()
        {
            if (!Owned) return;
            _velocityRecord = new(config.velocityRecordSize);

            movement.onDash.AddListener(OnDash);
            movement.onJump.AddListener(OnJump);
            movement.onGroundSlamLanded.AddListener((_) => OnGroundSlamLand());

            movement.onWalled.AddListener(OnWalled);
            movement.onUnwalled.AddListener(OnUnwalled);
        }

        private void OnWalled(Vector3 normal)
        {
            if (serverSide && isServer)
            {
                _walled = true;
                _wallNormal = normal;
            }
            else if (isOwned) CmdOnWalled(normal);
        }
        [Command]
        private void CmdOnWalled(Vector3 normal)
        {
            _walled = true;
            _wallNormal = normal;
        }

        private void OnUnwalled()
        {
            if (serverSide && isServer) _walled = false;
            else if (isOwned) CmdOnUnwalled();
        }
        [Command]
        private void CmdOnUnwalled() => _walled = false;

        private void OnDash()
        {
            _dashAudioSource.Play();

            if (serverSide && isServer) RpcOnDash();
            else if (isOwned) CmdOnDash();
        }
        [Command]
        private void CmdOnDash() => RpcOnDash();
        [ClientRpc]
        private void RpcOnDash()
        {
            if (Owned) return;
            _dashAudioSource.Play();
        }

        private void OnJump()
        {
            _jumpPrimaryAudioSource.Play();

            if (serverSide && isServer) RpcOnJump();
            else if (isOwned) CmdOnJump();
        }
        [Command]
        private void CmdOnJump() => RpcOnJump();
        [ClientRpc]
        private void RpcOnJump()
        {
            if (Owned) return;
            _jumpPrimaryAudioSource.Play();
        }

        private void OnGroundSlamLand()
        {
            _groundSlamAudioSource.Play();

            if (serverSide && isServer) RpcOnGroundSlamLand();
            else if (isOwned) CmdOnGroundSlamLand();
        }
        [Command]
        private void CmdOnGroundSlamLand() => RpcOnGroundSlamLand();
        [ClientRpc]
        private void RpcOnGroundSlamLand()
        {
            if (Owned) return;
            _groundSlamAudioSource.Play();
        }

        private void FixedUpdate()
        {
            if (!Owned) return;
            _velocity = transform.position - _prevPosition;

            if (_velocityRecord.Count == config.velocityRecordSize) _velocityRecord.Dequeue();
            _velocityRecord.Enqueue(_velocity);

            _prevPosition = transform.position;
        }

        private void Update()
        {
            // WALL SLIDE
            if (_walled)
            {
                _wallSlideAudioSource.transform.localPosition = -_wallNormal * movement.motor.Capsule.radius + Vector3.up * movement.motor.Capsule.height / 2f;
                _wallSlideAudioSource.volume = Mathf.Min(_wallSlideAudioSource.volume + config.wallSlideVolumeIncreaseRate * Time.deltaTime, config.wallSlideVolume);
            }
            else
                _wallSlideAudioSource.volume = Mathf.Lerp(_wallSlideAudioSource.volume, 0f, Time.deltaTime * config.wallSlideVolumeSmoothingSpeed);

            if (!Owned) return;

            // SKID
            if (_velocityRecord.Count > 0)
            {
                var oldVelocity = _velocityRecord.Peek();
                var diff = new Vector2(_velocity.x, _velocity.z).magnitude - new Vector2(oldVelocity.x, oldVelocity.z).magnitude;
                if (diff <= config.skidThreshold && movement.motor.GroundingStatus.IsStableOnGround && !_skidAudioSource.isPlaying)
                    OnSkid();
            }

            // LAND
            if (movement.motor.GroundingStatus.IsStableOnGround && !_wasOnGround) OnLand();
            _wasOnGround = movement.motor.GroundingStatus.IsStableOnGround;
        }

        private void OnSkid()
        {
            _skidAudioSource.Play();

            if (serverSide && isServer) RpcOnSkid();
            else if (isOwned) CmdOnSkid();
        }
        [Command]
        private void CmdOnSkid() => RpcOnSkid();
        [ClientRpc]
        private void RpcOnSkid()
        {
            if (Owned) return;
            _skidAudioSource.Play();
        }

        private void OnLand()
        {
            _landAudioSource.Play();

            if (serverSide && isServer) RpcOnLand();
            else if (isOwned) CmdOnLand();
        }
        [Command]
        private void CmdOnLand() => RpcOnLand();
        [ClientRpc]
        private void RpcOnLand()
        {
            if (Owned) return;
            _landAudioSource.Play();
        }
    }
}