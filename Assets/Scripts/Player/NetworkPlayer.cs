using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NetworkPlayer : NetworkBehaviour
{
    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";

    [Header("Components")]
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private ItemsReader _ir;

    [Header("Player")]
    [SerializeField] private float _maxHealth = 100;

    [Header("Movement Control")]
    [SerializeField, Tooltip("Base speed of the player")] private float _startSpeed = 50f;

    [Space(9)]

    [SerializeField, Tooltip("Каждые 20 миллисекунд акселерация будет увеличиваться на (0.02 * значение этого поля)")] private float _accelerationFactor = 10f;
    [SerializeField, Tooltip("Максимальное значение акселерации")] private float _maximumAcceleration = 30f;

    [Space(9)]

    [SerializeField, Tooltip("Чем меньше значение, тем больше скольжение у игрока. Так же наоборот"), Min(0)] private float _dragOnGround = 8.25f;

    [Header("Jumping Control")]
    [SerializeField, Tooltip("Сила прыжка"), Min(0)] private float _jumpForce = 11.25f;
    [SerializeField, Tooltip("Когда игрок не на земле, его скорость будет поделена на значение этого поля"), Min(1)] private float _airSpeedDivider = 6.25f;

    [Space(9)]

    [SerializeField, Tooltip("Когда игрок банихопит, его скорость постоянно плюсуется с этим значением каждый прыжок"), Min(0)] private float _bunnyHopFactor = 0.5f;
    [SerializeField, Tooltip("Если блять \nпосле прыжка проходит столько времени, то вся скорость с банихопа сбрасывается"), Min(0)] private float _bunnyHopTimeout = 0.35f;

    [Space(9)]

    [SerializeField, Tooltip("Сколько прыжков доступно игроку в воздухе?"), Min(0)] private int _availableAirJumps = 1;

    private int _jumpedTimesInAir;
    private float _airJumpTimeout;

    [Space(9)]

    [SerializeField, Tooltip("Койоти тайм"), Min(0)] private float _coyoteTime = 0.2f;
    [SerializeField, Tooltip("Джамп Баффер"), Min(0)] private float _jumpBuffer = 0.2f;

    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;

    [Header("Dashing Control")]
    [SerializeField, Tooltip("Сила рывка"), Min(0)] private float _dashForce = 50f;
    [SerializeField, Tooltip("Драг рывка"), Min(0)] private float _dashDrag = 6f;
    [SerializeField, Tooltip("Сколько длится деш"), Min(0)] private float _dashTime = 0.05f;
    [SerializeField, Tooltip("На сколько секунд игрок не сможет делать рывок после совершенного рывка"), Min(0)] private float _dashTimeout = 0.25f;
    [SerializeField, Tooltip("Сколько дешей доступно"), Min(0)] private int _dashesAvailable = 4;
    [SerializeField, Tooltip("Время перезарядки деша"), Min(0)] private float _dashReloadTime = 3f;

    private bool _isDashing;
    private bool _readyToDash;
    private bool _isDashReloading;
    private int _dashesRemaining;

    [Header("Ground Dashing Control")]
    [SerializeField, Tooltip("Сила рывка вниз"), Min(0)] private float _groundDashForce = 5f;

    [field: Header("Property Checking")]
    [field: SerializeField] public LayerMask MapLayers { get; private set; }
    [SerializeField] private float _groundCheckRadius;
    [SerializeField] private Vector3 _groundCheckOffset;

    [Header("Inputs")]
    public KeyCode JumpKey = KeyCode.Space;
    public KeyCode DashKey = KeyCode.LeftShift;
    public KeyCode GroundDashKey = KeyCode.LeftControl;

    private Vector3 _slopeNormal;

    private float _accel;
    private float _bhop;
    private float _bhopTimer;
    private Vector2 _inputs;
    private Vector3 _playerDirection;

    [HideInInspector] public bool IsMoving { get; private set; }
    [HideInInspector] public bool IsGrounded { get; private set; }

    private bool _wantToJump;
    private bool _wantToDash;
    private bool _wantToGroundDash;

    private Vector3 _playerSlopeDirection;

    private bool _isColliding;

    [Header("Particles")]
    [SerializeField, Tooltip("Не меняй")] private GameObject[] _particles;

    [Header("Sounds")]
    [SerializeField, Tooltip("Звук прыжка")] private AudioClip _jumpSound;
    [SerializeField, Tooltip("Звук рывка")] private AudioClip _dashSound;
    [SerializeField, Tooltip("Звук провального рывка")] private AudioClip _dashFailedSound;
    [SerializeField, Tooltip("Звук конца перезарядки рывка")] private AudioClip _dashReadySound;
    [SerializeField, Tooltip("Звук удара в землю")] private List<AudioClip> _groundSlamSounds = new List<AudioClip>();

    [Space(9)]

    [SerializeField, Tooltip("Звук получения урона")] private AudioClip _damageSound;
    [SerializeField, Tooltip("Локальный звук получения урона")] private AudioClip _localDamageSound;
    [SerializeField, Tooltip("Звук уведомления о том что игрок ударил кого то")] private AudioClip _hitLogSound;
    [SerializeField, Tooltip("Звук уведомления о том что игрок убил кого то")] private AudioClip _killLogSound;

    [Header("Materials")]
    [SerializeField, Tooltip("Не меняй")] private Material[] _normalMaterials = new Material[7];
    [SerializeField, Tooltip("Не меняй")] private Material[] _invincibleMaterials = new Material[7];

    [Header("Objects")]
    [SerializeField, Tooltip("Не меняй")] private Transform _orientation;
    [SerializeField, Tooltip("Не меняй")] private MeshRenderer _body;
    [SerializeField, Tooltip("Не меняй")] private GameObject _deathEffect;

    [HideInInspector] public Camera PlayerCamera;
    [HideInInspector] public CameraMovement PlayerMoveCamera;

    [HideInInspector] public bool AllowMovement;

    [Header("Info")]

    [SyncVar, SerializeField, ReadOnly] private int _score;
    [SyncVar, SerializeField, ReadOnly] private int _activity;

    public int Score { get => _score; }
    public int Activity { get => _activity; }

    [field: ReadOnly, SerializeField] public int Place { get; set; }

    [field: ReadOnly, SerializeField] public int Kills { get; private set; }
    [field: ReadOnly, SerializeField] public int Hits { get; private set; }

    [field: ReadOnly, SerializeField] public int Deaths { get; private set; }
    [field: ReadOnly, SerializeField] public int Traumas { get; private set; }

    [SyncVar, SerializeField, ReadOnly] private string _nickname;
    public string Nickname { get => _nickname; }

    [SyncVar, SerializeField, ReadOnly] private Color _color;
    public Color Color { get => _color; }

    [SyncVar, SerializeField, ReadOnly] private string _colorHex;
    public string ColorHEX { get => _colorHex; }

    [SyncVar, SerializeField, ReadOnly] private bool _initialized;
    public bool Initialized { get => _initialized; }

    [SyncVar, SerializeField, ReadOnly] private bool _invincible;
    public bool Invincible { get => _invincible; }

    [SyncVar, SerializeField, ReadOnly] private bool _isDead;
    public bool IsDead { get => _isDead; }

    public static NetworkPlayer LocalPlayer { get; private set; }

    [Command]
    public void CmdInitialize(string nickname, string color)
    {
        ColorUtility.TryParseHtmlString($"#{color}", out Color parsedColor);
        _colorHex = $"#{color}";
        _color = parsedColor;
        _nickname = nickname;
        _initialized = true;

        BodyToNormal();
    }

    #region HealthSystem

    [SyncVar, SerializeField, ReadOnly] private float _health;
    public float Health { get => _health; }

    private int _itemsUsed;

    private void BodyTo(Material[] mats)
    {
        if (isLocalPlayer) return;
        _body.materials = mats;
    }

    [ClientRpc] private void BodyToNormal() => BodyTo(_normalMaterials);
    [ClientRpc] private void BodyToInvincible() => BodyTo(_invincibleMaterials);

    public void Heal(float amount)
    {
        if (!isLocalPlayer) return;

        SetHealth(_health + amount);
    }

    public void TakeDamage(float amount)
    {
        if (!isLocalPlayer || _invincible) return;

        SoundSystem.Singleton.PlaySFX(new SoundTransporter(_damageSound), new SoundPositioner(transform.position), 0.95f, 1.05f, 1f);
        SoundSystem.PlayInterfaceSound(new SoundTransporter(_localDamageSound));
        Markers.Singleton.DoDamageMarker();

        SetHealth(_health - amount);

        Traumas++;
    }

    public void SetHealth(float amount)
    {
        if (!isLocalPlayer) return;

        if (amount > _maxHealth || amount < 0)
        {
            Debug.LogWarning($"{gameObject.name} attempted to set health out of bounds (target: {amount}, maximum: {_maxHealth}, minimum: 0)");
        }

        float clampedAmount = Mathf.Clamp(amount, 0, _maxHealth);

        float difference = _health - clampedAmount;

        if (difference == 0)
        {
            Debug.LogWarning("It's useless to change health");
            return;
        }

        CmdSetHealth(clampedAmount);

        StartCoroutine(nameof(OnHealthChanged), clampedAmount);
    }

    private IEnumerator OnHealthChanged(float amount)
    {
        yield return new WaitUntil(() => _health == amount); // на случай задержки синхронизации поля Health

        Hud.Singleton.TweenHealthToAmount(amount);

        if (_health <= 0)
        {
            OnDeath();
        }
    }

    private List<GameObject> _spawnedDeathEffects = new();

    private void OnDeath()
    {
        Action respawn = OnRespawn;
        CmdDie();

        _ir.LoseItem();
        _ir.RemoveAllMutations();

        AllowMovement = false;
        PlayerMoveCamera.BlockMovement = true;

        DisablePlayer(false);

        _rb.velocity = Vector3.zero;

        EverywhereCanvas.Singleton.StartDeathScreen(ref respawn);
        Deaths++;

        Hud.Singleton.SetDashes(0);
    }

    [Command]
    private void CmdDie()
    {
        _isDead = true;

        var newEffect = Instantiate(_deathEffect, transform.position - Vector3.up * 0.5f, _orientation.rotation);
        NetworkServer.Spawn(newEffect);
        _spawnedDeathEffects.Add(newEffect);
    }

    private void OnRespawn()
    {
        SetHealth(100);

        AllowMovement = true;
        PlayerMoveCamera.BlockMovement = false;

        DisablePlayer(true);
        ActivateInvincible(5f);

        _dashesRemaining = _dashesAvailable;
        Hud.Singleton.SetDashes(_dashesAvailable);

        StartCoroutine(nameof(Respawn));
        CmdRespawn();
    }

    [Command]
    private void CmdRespawn()
    {
        _isDead = false;

        foreach (var effect in _spawnedDeathEffects)
        {
            NetworkServer.Destroy(effect);
        }
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForFixedUpdate();
        var pos = NetworkManager.startPositions[UnityEngine.Random.Range(0, NetworkManager.startPositions.Count)].position;
        Debug.Log($"respawning player at {pos}");
        transform.position = pos;
    }

    [Command]
    private void CmdSetHealth(float amount) { _health = amount; }

    private void DisablePlayer(bool enable)
    {
        gameObject.layer = enable ? 12 : 13;

        CmdDisablePlayer(enable);
    }

    [Command]
    private void CmdDisablePlayer(bool enable) { RpcDisablePlayer(enable); }

    [ClientRpc]
    private void RpcDisablePlayer(bool enable)
    {
        _body.GetComponent<MeshRenderer>().enabled = enable;

        if (isLocalPlayer) return;

        gameObject.layer = enable ? 11 : 13;
    }

    [Command(requiresAuthority = false)]
    public void CmdHitPlayer(NetworkIdentity owner, float damage)
    {
        if (_health <= 0)
        {
            Debug.LogWarning("Can't hit a dead player");
            return;
        }

        if (_invincible)
        {
            Debug.LogWarning("Can't hit invincible player");
            return;
        }

        TRpcHitPlayer(connectionToClient, damage);
        TRpcLogHit(owner.connectionToClient, _health - damage <= 0);
    }

    [TargetRpc]
    private void TRpcHitPlayer(NetworkConnectionToClient target, float damage)
    {
        TakeDamage(damage);
    }

    [TargetRpc]
    private void TRpcLogHit(NetworkConnectionToClient target, bool gonnaDie)
    {
        NetworkPlayer networkPlayer = LocalPlayer;

        if (networkPlayer == this)
        {
            Debug.LogWarning("Can't log hit for self-damage");
            return;
        }

        networkPlayer.LogHit();

        if (gonnaDie)
        {
            networkPlayer.LogKill();
        }
    }

    #endregion

    private void OnValidate() // этот метод вызывается когда в инспекторе меняется поле или после компиляции скрипта
    {
        if (TryGetComponent<Rigidbody>(out _rb)) // важные параметры для ригидбоди
        {
            _rb.interpolation = RigidbodyInterpolation.Interpolate; // чтоб было плавно
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // чтоб была не лагучая коллизия
            _rb.freezeRotation = true; // чтоб игрока не вертело как ебанутого
        }

        TryGetComponent<ItemsReader>(out _ir);
    }

    private void Start()
    {
        InitializeVariables();
    }

    private void InitializeVariables()
    {
        ResetDash();
        AllowDash();
        AllowMovement = true;
        _dashesRemaining = _dashesAvailable;
    }

    public override void OnStartLocalPlayer() // то же самое что и старт, только для локального игрока
    {
        LocalPlayer = this;
        Initialize();
    }

    private void Initialize() // уничтожаем другие камеры на сцене и создаем себе новую
    {
        string nickname = PlayerPrefs.GetString("PlayerNicknameValue");
        var color = PlayerPrefs.GetString("PlayerColorHEX", "FFFFFF");
        CmdInitialize(nickname, color);

        SetupPlayerAndGameObject();

        Hud.Singleton.HealthSlider().maxValue = _maxHealth;
        Hud.Singleton.SetDashes(_dashesAvailable);
        Leaderboard.Singleton.StartLeaderboard();

        DestroyCameras();
        GameObject newCamera = new("Player Camera", typeof(CameraMovement))
        {
            tag = "MainCamera"
        };
        PlayerCamera = newCamera.AddComponent<Camera>();
        InitializeCamera();

        PlayerCurrentStats.Singleton.ResetStats();
        PlayerMutationStats.Singleton.ResetStats();
    }

    private void SetupPlayerAndGameObject()
    {
        SetHealth(_maxHealth);

        _body.gameObject.SetActive(false);
        gameObject.layer = 12;
    }

    private static void DestroyCameras()
    {
        Camera[] otherCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Camera camera in otherCameras)
        {
            Destroy(camera.gameObject);
        }
    }

    private void InitializeCamera() // инициализируем камеру (задаем параметры по дефолту и добавляем нужные компоненты)
    {
        PlayerCamera.fieldOfView = 80;
        PlayerCamera.nearClipPlane = 0.01f;

        GameObject camera = PlayerCamera.gameObject;
        GameObject cameraHolder = new GameObject("Camera Holder");

        PlayerMoveCamera = camera.GetComponent<CameraMovement>();
        camera.AddComponent<AudioListener>();
        camera.GetComponent<CameraMovement>().Player = this;

        Transform cameraHolderTransform = cameraHolder.transform;
        camera.GetComponent<CameraMovement>().CamHolder = cameraHolderTransform;

        cameraHolderTransform.SetParent(gameObject.transform);
        cameraHolderTransform.localPosition = Vector3.zero;

        camera.transform.SetParent(cameraHolderTransform);
        camera.transform.localPosition = Vector3.up * 0.5f;

        PlayerMoveCamera.Orientation = _orientation;
    }

    [Command]
    private void CmdChangeScore(int amount)
    {
        _score += amount;

        Leaderboard.Singleton.UpdateLeaderboard();
        SceneGameManager.Singleton.RpcForceClientsForLeaderboardUpdate();
    }

    [Command]
    private void CmdChangeActivity(int amount)
    {
        _activity += amount;

        Leaderboard.Singleton.UpdateLeaderboard();
        SceneGameManager.Singleton.RpcForceClientsForLeaderboardUpdate();
    }

    [Server]
    public void SetLeaderboardStats(int score, int activity)
    {
        _score = score;
        _activity = activity;
    }

    private void Update()
    {
        if (!isLocalPlayer) return; // эта строка заставляет выйти из метода, если мы не являемся локальным игроком

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        DebugKeys();
#endif

        ReceiveInputs();
        JumpHandle();

        if (_wantToDash && _readyToDash)
        {
            if (_isDashReloading)
            {
                SoundSystem.PlayInterfaceSound(new SoundTransporter(_dashFailedSound), volume: 0.65f);
                Hud.Singleton.Shake(new ShakeEffect(0.2f, 15f));
            }
            else Dash();
        }

        if (_wantToGroundDash)
        {
            GroundDash();
        }

        HandleNicknameDisplay();
    }

    private void JumpHandle()
    {
        if (IsGrounded)
        {
            _coyoteTimeCounter = _coyoteTime;

            _airJumpTimeout = 0.5f;
        }
        else
        {
            _coyoteTimeCounter -= Time.deltaTime;

            if (_airJumpTimeout > 0)
            {
                _airJumpTimeout -= Time.deltaTime;
            }
        }

        if (_wantToJump)
        {
            _jumpBufferCounter = _jumpBuffer;
        }
        else
        {
            _jumpBufferCounter -= Time.deltaTime;
        }

        bool jumpRequested = _coyoteTimeCounter > 0f;
        bool jumpIsPossible = _jumpBufferCounter > 0f;

        if (jumpRequested && jumpIsPossible)
        {
            Jump();

            _jumpBufferCounter = 0f;
        }

        if (_wantToJump && !IsGrounded && !jumpRequested && _jumpedTimesInAir < _availableAirJumps && _airJumpTimeout > 0)
        {
            Jump();

            _jumpedTimesInAir++;

            if (_jumpedTimesInAir == _availableAirJumps)
            {
                StartCoroutine(nameof(WaitForLand));
            }
        }

        if (Input.GetKeyUp(JumpKey))
        {
            _coyoteTimeCounter = 0;
        }
    }

    private IEnumerator WaitForLand()
    {
        yield return new WaitUntil(() => IsGrounded);

        _jumpedTimesInAir = 0;
    }

    private void HandleNicknameDisplay()
    {
        Transform cameraTransform = PlayerCamera.transform;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, 1500f, LayerMask.GetMask("Player", "Map")))
        {
            if (hit.transform.TryGetComponent(out NetworkPlayer player))
            {
                EverywhereCanvas.Singleton.SwitchNicknameVisibility(true, player.Color, player.Nickname);
            }
            else
            {
                EverywhereCanvas.Singleton.SwitchNicknameVisibility(false, Color.clear);
            }
        }
        else
        {
            EverywhereCanvas.Singleton.SwitchNicknameVisibility(false, Color.clear);
        }
    }

    private void DebugKeys()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Input.GetKeyDown(KeyCode.Minus))
        {
            TakeDamage(5);
        }

        if (Input.GetKeyDown(KeyCode.Equals))
        {
            Heal(5);
        }
#endif
    }

    private void Jump()
    {
        if (!AllowMovement || Chat.Singleton.Focused || PauseMenu.Singleton.PauseMenuOpened) return;

        PlayerCurrentStats.Singleton.Bounce = _jumpForce;
        _rb.velocity = new Vector3(_rb.velocity.x, PlayerCurrentStats.Singleton.Bounce + PlayerMutationStats.Singleton.Bounce, _rb.velocity.z);

        CheckForBhop();
        MakeJumpEffects();
    }

    private void CheckForBhop()
    {
        if (_bhopTimer > 0) // если наш банихоп не в таймауте
        {
            _bhop += _bunnyHopFactor; // то при прыжке добавляем скорости
        }
        else
        {
            _bhop = 0; // иначе сбрасываем скорость
        }

        _bhopTimer = _bunnyHopTimeout;
    }

    private void MakeJumpEffects()
    {
        SoundSystem.Singleton.PlaySFX(new SoundTransporter(_jumpSound), new SoundPositioner(transform.position), 0.85f, 1f, 0.6f);
        CmdSpawnParticle(0);
    }

    [Command]
    private void CmdSpawnParticle(int idx) // охуевший метод спавна партиклов спасибо миррор
    {
        GameObject particle = Instantiate(_particles[idx], transform.position, _particles[idx].transform.rotation * _body.transform.rotation);

        NetworkServer.Spawn(particle, gameObject);
    }

    private void ReceiveInputs() // ТУТ мы записываем значения в переменные связанные с инпутом, кнопками на клавиатуре, мышке и прочей хуйне
    {
        _inputs = GetAxisInputs();
        IsMoving = _inputs.magnitude > 0;

        IsGrounded = _isColliding && CheckForGrounded();

        _wantToJump = Input.GetKeyDown(JumpKey);
        _wantToDash = Input.GetKeyDown(DashKey);
        _wantToGroundDash = Input.GetKeyDown(GroundDashKey);

        _playerDirection = _orientation.forward * _inputs.y + _orientation.right * _inputs.x;
    }

    public Vector2 GetAxisInputs() // можно было и без этого метода но тут чисто ради выебонов
    {
        if (!AllowMovement || Chat.Singleton.Focused || PauseMenu.Singleton.PauseMenuOpened) return Vector2.zero;

        return new Vector2(Input.GetAxisRaw(HORIZONTAL), Input.GetAxisRaw(VERTICAL));
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return; // эта строка заставляет выйти из метода, если мы не являемся локальным игроком

        IdleHandle();
        RigidbodyMovement();

        if (_bhopTimer > 0)
        {
            _bhopTimer -= Time.fixedDeltaTime;
        }
    }

    private bool CheckForGrounded()
    {
        var grounded = Physics.CheckSphere(transform.position + _groundCheckOffset, _groundCheckRadius, MapLayers.value, QueryTriggerInteraction.Ignore);
        return grounded;
    }

    public bool IsSloped() // не тупле метод не ахуевший сука
    {
        if (!IsGrounded) return false;

        Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 3f, MapLayers.value, QueryTriggerInteraction.Ignore);
        _slopeNormal = hit.normal;

        return _slopeNormal != Vector3.up;
    }

    private void IdleHandle()
    {
        _rb.useGravity = !IsSloped();
    }

    private void RigidbodyMovement() // тут мы двигаем перса по оси X и Z (а так же делаем слоуп хандлинг по оси Y)
    {
        if (!AllowMovement) { _rb.velocity = Vector3.up * _rb.velocity.y; return; }

        if (!IsMoving)
            (_bhop, _accel) = (0, 0);
        else
        {
            _accel += Time.deltaTime * _accelerationFactor;
            _accel = Mathf.Clamp(_accel, 0, _maximumAcceleration);
        }

        HandleDrag();

        float angleBoost = Mathf.Abs
        (
            Vector3.Angle(Vector3.up, _slopeNormal) * 1.75f
        );

        Vector3 targetDirection = IsSloped() ? Vector3.ProjectOnPlane(_playerDirection, _slopeNormal) : _playerDirection;

        PlayerCurrentStats.Singleton.Speed = _startSpeed + _accel + _bhop + angleBoost;
        if (!IsGrounded)
        {
            PlayerCurrentStats.Singleton.Speed /= _airSpeedDivider;
        }

        _rb.AddForce(targetDirection * (PlayerCurrentStats.Singleton.Speed + PlayerMutationStats.Singleton.Speed));
    }

    private void HandleDrag()
    {
        if (_isDashing)
        {
            _rb.drag = _dashDrag;
            return;
        }

        _rb.drag = IsGrounded ? _dragOnGround : 0;
    }

    private void Dash()
    {
        if (!AllowMovement || Chat.Singleton.Focused || PauseMenu.Singleton.PauseMenuOpened) return;

        _dashesRemaining--;
        Hud.Singleton.RemoveLastDashDot();

        if (_dashesRemaining == 0) StartCoroutine(nameof(DashReloadLogic));

        _isDashing = true;
        Invoke(nameof(ResetDash), _dashTime);

        Vector3 targetDirection = IsMoving ? _playerDirection : new Vector3(_orientation.forward.x, 0, _orientation.forward.z);
        _rb.velocity = new(_rb.velocity.x + targetDirection.x * _dashForce, _rb.velocity.y < 0 ? 0 : _rb.velocity.y, _rb.velocity.z + targetDirection.z * _dashForce);
        TimeoutDash(_dashTimeout);

        SoundSystem.Singleton.PlaySFX(new SoundTransporter(_dashSound), new SoundPositioner(transform.position), 0.85f, 1f, 0.6f);
        CmdSpawnParticle(1);
    }

    private IEnumerator DashReloadLogic()
    {
        float elapsed = 0.0f;

        _isDashReloading = true;

        while (elapsed < _dashReloadTime && !IsGrounded)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        _dashesRemaining = _dashesAvailable;
        _isDashReloading = false;
        Hud.Singleton.SetDashes(_dashesAvailable);
        SoundSystem.PlayInterfaceSound(new SoundTransporter(_dashReadySound), volume: 0.65f);
    }

    private void GroundDash()
    {
        if (!AllowMovement || Chat.Singleton.Focused || PauseMenu.Singleton.PauseMenuOpened || IsGrounded) return;

        float targetForce = _rb.velocity.y <= -1f ? -_groundDashForce + (_rb.velocity.y * 3) : -_groundDashForce;
        var wasHit = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1000f, MapLayers);
        if (!wasHit) return;

        _rb.velocity = new Vector3(_rb.velocity.x, targetForce - hit.distance, _rb.velocity.z);

        StartCoroutine(nameof(EffectWhenLanded));
    }

    private IEnumerator EffectWhenLanded()
    {
        yield return new WaitUntil(() => IsGrounded);

        SoundSystem.Singleton.PlaySFX(new SoundTransporter(_groundSlamSounds), new SoundPositioner(transform.position), 0.85f, 1f, 0.6f);
        PlayerMoveCamera.Shake(0.15f, 0.15f);
        CmdSpawnParticle(2);
    }

    public void LogHit()
    {
        SoundSystem.PlayInterfaceSound(new SoundTransporter(_hitLogSound));
        PlayerMoveCamera.Shake(strength: 0.1f);
        Markers.Singleton.DoHitMarker();

        CmdChangeScore(1);

        Hits++;
    }

    public void LogKill()
    {
        SoundSystem.PlayInterfaceSound(new SoundTransporter(_killLogSound), volume: 3);
        PlayerMoveCamera.Shake(strength: 0.25f);
        EverywhereCanvas.Singleton.LogKill();

        CmdChangeScore(2);

        Kills++;
    }

    public void OnItemUsed()
    {
        CmdChangeActivity(1);

        _itemsUsed++;

        if (Convert.ToSingle(_itemsUsed) % 30f == 0)
        {
            CmdChangeActivity(1);
        }
    }

    private void ActivateInvincible(float time)
    {
        CmdSetInvincible(true);
        Overlays.Singleton.BeginInvincibleOverlay();

        Invoke(nameof(ResetInvincible), time);
    }

    private void ResetInvincible()
    {
        CmdSetInvincible(false);
        Overlays.Singleton.EndInvincibleOverlay();
    }

    [Command]
    private void CmdSetInvincible(bool invincible)
    {
        _invincible = invincible;

        if (invincible) BodyToInvincible();
        else BodyToNormal();
    }

    private void TimeoutDash(float time)
    {
        _readyToDash = false;
        Invoke(nameof(AllowDash), time);
    }

    private void ResetDash() => _isDashing = false;
    private void AllowDash() => _readyToDash = true;

    private void OnCollisionStay(Collision other) => _isColliding = true;
    private void OnCollisionExit(Collision other) => _isColliding = false;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(transform.position + _groundCheckOffset, _groundCheckRadius);
    }
}

public class PlayerMutationStats : PlayerStats // статы, на которые влияют мутации на самом деле (но игроки тупые обезьяны они об этом не узнают)
{
    public static PlayerCurrentStats Singleton = new();
}

public class PlayerCurrentStats : PlayerStats // эти статы сделаны для защиты (чтоб никакие хакеры хуякеры не могли получить всего игрока)
{
    public static PlayerCurrentStats Singleton = new();
}

public class PlayerStats
{
    public float Speed;
    public float Bounce;
    public byte Luck;
    public float Damage;

    public void ResetStats()
    {
        Speed = 0;
        Bounce = 0;
        Luck = 0;
        Damage = 0;
    }
}
