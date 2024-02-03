using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    [Header("Custom")]
    [SerializeField] private Transition _transition;

    private bool _sceneChanged;
    private bool _justConnected;

    public override void Awake()
    {
        base.Awake();

        foreach (var canvas in GetEverywhereCanvases())
        {
            canvas.Active = false;
            canvas.Reset();
        }

        List<GameObject> projectiles = Resources.LoadAll<GameObject>("Items/Projectiles").ToList();
        List<GameObject> netPrefabs = Resources.LoadAll<GameObject>("NetworkedPrefabs").ToList();
        netPrefabs.AddRange(projectiles);
        spawnPrefabs = netPrefabs;

        _transition = Transition.Singleton();
        SceneManager.activeSceneChanged += (Scene a, Scene b) => _sceneChanged = true;
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        StartCoroutine(OnDisconnect());
    }

    private IEnumerator OnDisconnect()
    {
        _justConnected = false;

        foreach (var canvas in GetEverywhereCanvases())
        {
            canvas.Reset();
            canvas.OnDisconnect();

            canvas.Active = false;
        }

        yield return new WaitUntil(() => _sceneChanged);
        _sceneChanged = false;

        OnEnterMenu();
    }

    private void OnEnterMenu()
    {
        _transition.AwakeTransition(TransitionMode.Out);
        Cursor.lockState = CursorLockMode.None;
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        _justConnected = true;

        foreach (var canvas in GetEverywhereCanvases())
        {
            canvas.Reset();
            canvas.Active = true;
        }

        StartCoroutine(nameof(WaitForClientLoad));
    }

    private IEnumerator WaitForClientLoad()
    {
        yield return new WaitUntil(() => NetworkClient.localPlayer != null);

        Cursor.lockState = CursorLockMode.Locked;
        _transition.AwakeTransition(TransitionMode.Out);

        SceneGameManager sceneGameManager = SceneGameManager.Singleton;

        EverywhereCanvas.Singleton.SwitchUIGameState(GameInfo.Singleton.CurrentCanvasGameState);
        EverywhereCanvas.Singleton.SetMapVoting(GameInfo.Singleton.IsVotingTime, false);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        NetworkPlayer player = conn.identity.GetComponent<NetworkPlayer>();
        GameLoop gameLoop = GameLoop.Singleton;

        if (!gameLoop.ExitedPlayers.ContainsKey(player.Nickname))
        {
            gameLoop.ExitedPlayers.Add(player.Nickname, (player.Score, player.Activity));
        }

        base.OnServerDisconnect(conn);

        SceneGameManager.Singleton.RpcForceClientsForLeaderboardUpdate();
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        StartCoroutine(RegisterNewClient(conn));
    }

    private IEnumerator RegisterNewClient(NetworkConnectionToClient conn)
    {
        yield return new WaitUntil(() => conn.identity != null);

        NetworkPlayer player = conn.identity.GetComponent<NetworkPlayer>();
        GameLoop gameLoop = GameLoop.Singleton;

        yield return new WaitUntil(() => player.Initialized);

        if (gameLoop.ExitedPlayers.ContainsKey(player.Nickname))
        {
            (int score, int activity) value = gameLoop.ExitedPlayers[player.Nickname];

            player.SetLeaderboardStats(value.score, value.activity);

            gameLoop.ExitedPlayers.Remove(player.Nickname);
        }

        SceneGameManager.Singleton.RpcForceClientsForLeaderboardUpdate();
    }

    private static IEverywhereCanvas[] GetEverywhereCanvases()
    {
        return FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IEverywhereCanvas>().ToArray();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        StartCoroutine(nameof(WaitForGameLoop));
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        if (GameLoop.Singleton != null) GameLoop.Singleton.SetSceneLoaded(true);
    }

    public override void OnServerChangeScene(string newSceneName)
    {
        base.OnServerChangeScene(newSceneName);

        if (GameLoop.Singleton != null) GameLoop.Singleton.SetSceneLoaded(false);
    }

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();

        StartCoroutine(nameof(WaitForPlayerAfterSceneChanged));
    }

    private IEnumerator WaitForPlayerAfterSceneChanged()
    {
        yield return new WaitUntil(() => NetworkClient.localPlayer != null);

        MusicSystem.StartMusic(GameInfo.Singleton.CurrentMusicGameState, GameInfo.Singleton.CurrentMusicOffset, GameInfo.Singleton.CurrentMusicIndex);
        _transition.AwakeTransition(TransitionMode.Out);

        if (GameInfo.Singleton.IsLobby && !_justConnected)
        {
            EverywhereUIController.Singleton.ExposeResults();
            EverywhereCanvas.Singleton.SwitchUIGameState(CanvasGameState.Lobby);
        }

        if (_justConnected) _justConnected = false;
    }

    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        ResultsWindow.Singleton.SetWindow(false);
    }

    private IEnumerator WaitForGameLoop()
    {
        yield return new WaitUntil(() => GameLoop.Singleton);

        GameLoop.Singleton.StartLoop();
    }
}
