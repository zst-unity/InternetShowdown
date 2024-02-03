using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoop : NetworkBehaviour
{
    public static GameLoop Singleton { get; private set; }
    [SerializeField] private GameInfo _gameInfo;

    [Header("Length")]
    [SerializeField, Min(10), Tooltip("Break time length in seconds")] private int _breakLength = 60;
    [SerializeField, Min(20), Tooltip("Large break time length in seconds")] private int _largeBreakLength = 180;
    [SerializeField, Min(30), Tooltip("Preparation length in seconds")] private int _prepareLength = 10;
    [SerializeField, Min(30), Tooltip("Round length in seconds")] private int _roundLength = 340;

    [Header("Other")]
    [SerializeField, Min(2), Tooltip("How many rounds does the large break time come after?")] private int _roundsToLargeBreak = 5;
    [SerializeField, Min(10), Tooltip("Seconds when counter turns yellow")] private int _attentionTimeYellow = 60;
    [SerializeField, Min(10), Tooltip("Seconds when counter turns red")] private int _attentionTimeRed = 10;

    [Header("Voting")]
    [SerializeField, Scene] private List<string> _maps = new();
    [SerializeField, Min(5), Tooltip("Time before the voting starts")] private int _preVotingTime = 10;
    [SerializeField, Min(5), Tooltip("Voting length in seconds")] private int _votingTime = 15;

    private Dictionary<string, int> _votes = new();
    private string _votedMap;
    private bool _isSceneLoaded;
    private bool _isSkipNeeded;
    private int _timeCounter;
    private int _repeatSeconds;
    private int _currentGamesPlayed;

    public Dictionary<string, (int score, int activity)> ExitedPlayers = new();

    public void SetSceneLoaded(bool loaded) => _isSceneLoaded = loaded;

    [Server]
    public void AddMapVote(string mapName)
    {
        if (_votes.ContainsKey(mapName))
            _votes[mapName]++;
        else
            _votes.Add(mapName, 1);
    }

    private void Awake()
    {
        if (FindObjectsByType<GameLoop>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            Singleton = this;
            DontDestroyOnLoad(transform);
        }
    }

    [ServerCallback]
    public void StartLoop()
    {
        StartCoroutine(nameof(Loop));
    }

    [ServerCallback]
    private void Update()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Backspace)) _isSkipNeeded = true;
#endif
    }

    private IEnumerator HandleMapVoting()
    {
        SceneGameManager.Singleton.RpcSetMapVoting(false, false);

        yield return new WaitForSeconds(_preVotingTime);

        GameInfo.Singleton.IsVotingTime = true;

        SceneGameManager.Singleton.RpcSetMapVoting(true, true);
        SceneGameManager.Singleton.RpcPlayVotingSound(true);

        yield return new WaitForSeconds(_votingTime);

        SceneGameManager.Singleton.RpcSetMapVoting(false, true);
        SceneGameManager.Singleton.RpcPlayVotingSound(false);

        OnVotingEnd();
    }

    private void OnVotingEnd()
    {
        GameInfo.Singleton.IsVotingTime = false;

        if (_votes.Count == 0)
        {
            Debug.LogWarning("Seems like nobody voted for map");

            SceneGameManager.Singleton.RpcOnVotingEnd("Nobody voted :(");

            return;
        }

        List<KeyValuePair<string, int>> _votesList = _votes.ToList();
        _votesList.Sort((current, next) => current.Value > next.Value ? -1 : 1);

        _votedMap = _votesList.First().Key;

        string votingEndMessage = $"{Path.GetFileNameWithoutExtension(_votedMap).ToSentence()} won!";

        SceneGameManager.Singleton.RpcOnVotingEnd(votingEndMessage);
    }

    private void CancelVoting()
    {
        StopCoroutine(nameof(HandleMapVoting));

        SceneGameManager.Singleton.RpcSetMapVoting(false, false);
        SceneGameManager.Singleton.RpcPlayVotingSound(false);

        OnVotingEnd();
    }

    private struct ColorFrom
    {
        public Color Color;
        public int From;

        public ColorFrom(Color color, int from)
        {
            Color = color;
            From = from;
        }
    }

    private IEnumerator Timer(List<ColorFrom> colors = null, int soundFrom = -1, int prepareFrom = -1)
    {
        Color targetColor = Color.white;
        bool playSound = false;

        for (int i = 0; i < _repeatSeconds; i++)
        {
            if (_isSkipNeeded)
            {
                _timeCounter = 0;
                _isSkipNeeded = false;

                if (GameInfo.Singleton.IsVotingTime)
                    CancelVoting();
                else
                    StopCoroutine(nameof(HandleMapVoting));

                break;
            }

            playSound = _timeCounter <= soundFrom;

            if (colors != null)
            {
                foreach (var color in colors)
                {
                    if (_timeCounter <= color.From)
                    {
                        targetColor = color.Color;
                        break;
                    }
                }
            }

            if (_timeCounter == prepareFrom) SceneGameManager.Singleton.RpcPrepareText(prepareFrom);

            OnTimeCounterUpdate(_timeCounter, targetColor, playSound);
            _timeCounter--;

            yield return new WaitForSecondsRealtime(1f);

        }

        OnTimeCounterUpdate(_timeCounter, targetColor, playSound);

        yield return new WaitForSecondsRealtime(1f);
    }

    private IEnumerator Loop()
    {
        WaitUntil _waitForSceneLoaded = new(() => _isSceneLoaded);

        _currentGamesPlayed = 1;

        GameObject newGameInfo = Instantiate(_gameInfo.gameObject);
        NetworkServer.Spawn(newGameInfo);

        while (NetworkServer.active)
        {
            yield return _waitForSceneLoaded;

            GameInfo.Singleton.CurrentMusicIndex = MusicSystem.GetIndex(MusicGameState.Lobby);
            GameInfo.Singleton.StartMusicOffset();

            if (_currentGamesPlayed % _roundsToLargeBreak == 0)
                SetGameState(GameState.LargeBreak, CanvasGameState.Lobby, MusicGameState.Lobby, _largeBreakLength);
            else
            {
                SetGameState(GameState.Break, CanvasGameState.Lobby, MusicGameState.Lobby, _breakLength);
                _timeCounter = _breakLength;
            }

            StartCoroutine(nameof(HandleMapVoting));

            yield return StartCoroutine(Timer(soundFrom: 10));

            SceneGameManager.Singleton.RpcTransition(TransitionMode.In);
            OnTimeCounterUpdate(null, Color.gray, false);
            yield return new WaitForSeconds(Transition.Singleton().FullDurationIn);

            GameInfo.Singleton.StopMusicOffset();

            LoadMatch();
            yield return _waitForSceneLoaded;

            GameInfo.Singleton.CurrentMusicIndex = MusicSystem.GetIndex(MusicGameState.Match);
            GameInfo.Singleton.StartMusicOffset();

            SetGameState(GameState.Prepare, CanvasGameState.Lobby, MusicGameState.Match, _prepareLength);

            yield return new WaitForSeconds(0.75f);
            yield return StartCoroutine(Timer(new List<ColorFrom>() { new(Color.gray, _repeatSeconds) }, _repeatSeconds, 3));
            StartMatch();

            List<ColorFrom> colorsFrom = new()
            {
                new(ColorISH.Red, _attentionTimeRed),
                new(ColorISH.Yellow, _attentionTimeYellow)
            };

            yield return StartCoroutine(Timer(colorsFrom, 60));

            StopMatch();
            SetGameState(GameState.MatchEnded, CanvasGameState.Game, MusicGameState.Match);
            yield return new WaitForSeconds(5f);

            _currentGamesPlayed++;
            GameInfo.Singleton.StopMusicOffset();

            SceneGameManager.Singleton.RpcTransition(TransitionMode.In);
            yield return new WaitForSeconds(Transition.Singleton().FullDurationIn);

            TimeToBreak();
        }
    }

    private void SetGameState(GameState state, CanvasGameState uiState, MusicGameState musicState, int time = 0)
    {
        GameInfo.Singleton.CurrentGameState = state;
        GameInfo.Singleton.CurrentCanvasGameState = uiState;
        GameInfo.Singleton.CurrentMusicGameState = musicState;

        _timeCounter = time;
        _repeatSeconds = _timeCounter;
    }

    private void LoadMatch()
    {
        _votes.Clear();

        if (string.IsNullOrEmpty(_votedMap))
        {
            Debug.LogWarning($"Voted Map is empty, loading random map instead");

            string randomMap = _maps[Random.Range(0, _maps.Count)];
            NetworkManager.singleton.ServerChangeScene(randomMap);

            return;
        }

        NetworkManager.singleton.ServerChangeScene(_votedMap);
    }

    private void StartMatch()
    {
        SetGameState(GameState.Match, CanvasGameState.Game, MusicGameState.Match, _roundLength);
        SceneGameManager.Singleton.RpcOnMatchStarted();

        FindFirstObjectByType<ItemSpawner>().StartSpawnProcess();
    }

    private void StopMatch()
    {
        ItemSpawner.Singleton.StopSpawnProcess();
        ItemSpawner.Singleton.DestroyAll();

        ProjectileBase[] allProjectiles = FindObjectsByType<ProjectileBase>(FindObjectsSortMode.None);
        foreach (var projectile in allProjectiles) { NetworkServer.Destroy(projectile.gameObject); }

        SceneGameManager.Singleton.RpcHideDeathScreen();
        SceneGameManager.Singleton.RpcRemoveMutations();
        SceneGameManager.Singleton.RpcAllowMovement(false);
        SceneGameManager.Singleton.RpcOnMatchEnd();

        ExitedPlayers.Clear();
    }

    private void TimeToBreak()
    {
        NetworkManager.singleton.ServerChangeScene("Lobby");
    }

    public void OnTimeCounterUpdate(int? counter, Color color, bool playSound)
    {
        if (SceneGameManager.Singleton == null) return;

        SceneGameManager.Singleton.RpcOnTimeCounterUpdate(counter, color, playSound);
    }
}

public enum GameState
{
    Break,
    LargeBreak,
    Prepare,
    Match,
    MatchEnded
}
