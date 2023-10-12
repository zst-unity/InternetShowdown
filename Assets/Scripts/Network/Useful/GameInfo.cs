using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInfo : NetworkBehaviour
{
    public static GameInfo Singleton;
    private void Awake()
    {
        DontDestroyOnLoad(gameObject.transform);
        Singleton = this;
    }

    [SyncVar, ReadOnly] public GameState CurrentGameState;
    [SyncVar, ReadOnly] public CanvasGameState CurrentCanvasGameState;
    [SyncVar, ReadOnly] public MusicGameState CurrentMusicGameState;

    [SyncVar, ReadOnly] public int? CurrentMusicIndex;
    [SyncVar, ReadOnly] public float CurrentMusicOffset;

    [SyncVar, ReadOnly] public bool IsVotingTime;

    public bool IsLobby
    {
        get => SceneManager.GetActiveScene().buildIndex == 2;
    }

    [Server]
    public void StartMusicOffset()
    {
        StartCoroutine(nameof(MusicTimerLogic));
    }

    [Server]
    public void StopMusicOffset()
    {
        StopCoroutine(nameof(MusicTimerLogic));
        CurrentMusicOffset = 0;
    }

    private IEnumerator MusicTimerLogic()
    {
        CurrentMusicOffset = 0;

        while (NetworkServer.active)
        {
            CurrentMusicOffset += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
    }
}
