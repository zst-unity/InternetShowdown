using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Mirror;
using UnityEngine;

public class SceneGameManager : NetworkBehaviour
{//
    public static SceneGameManager Singleton { get; private set; }

    [SerializeField] private List<AudioClip> _clockTicks = new();

    [Space(9)]

    [SerializeField] private AudioClip _votingStart;
    [SerializeField] private AudioClip _votingEnd;

    private void Awake() => Singleton = this;

    [ClientRpc]
    public void RpcOnTimeCounterUpdate(int? counter, Color color, bool playSound)
    {
        string exposedTimeCounter = "";

        if (counter != null)
        {
            int minutes = TimeSpan.FromSeconds(counter ?? 0).Minutes;
            int seconds = TimeSpan.FromSeconds(counter ?? 0).Seconds;

            string exposedSeconds = seconds < 10 ? "0" + seconds.ToString() : seconds.ToString();

            exposedTimeCounter = $"{minutes}:{exposedSeconds}";
        }

        EverywhereCanvas.Singleton.Timer.text = exposedTimeCounter;
        EverywhereCanvas.Singleton.Timer.color = color;
        EverywhereCanvas.Singleton.Timer.transform.localScale = Vector2.one * 1.075f;
        EverywhereCanvas.Singleton.Timer.transform.DOScale(Vector2.one, 0.5f).SetEase(Ease.OutElastic);

        if (!playSound) return;
        SoundSystem.PlayInterfaceSound(new SoundTransporter(_clockTicks), volume: 0.225f);
    }

    [ClientRpc]
    public void RpcPrepareText(int fromCount)
    {
        EverywhereCanvas.Singleton.PreMatchText(fromCount);
    }

    [ClientRpc]
    public void RpcAllowMovement(bool allow)
    {
        NetworkPlayer.LocalPlayer.AllowMovement = allow;
    }

    [ClientRpc]
    public void RpcRemoveMutations()
    {
        NetworkClient.localPlayer.GetComponent<ItemsReader>().RemoveAllMutations();
    }

    [ClientRpc]
    public void RpcShakeAll(float duration, float time)
    {
        NetworkPlayer.LocalPlayer.PlayerMoveCamera.Shake(duration, time);
    }

    [ClientRpc]
    public void RpcTransition(TransitionMode mode)
    {
        Transition.Singleton().AwakeTransition(mode);
    }

    [ClientRpc]
    public void RpcHideDeathScreen()
    {
        EverywhereCanvas.Singleton.HideDeathScreen();
    }

    [ClientRpc]
    public void RpcSetMapVoting(bool enable, bool fade)
    {
        EverywhereCanvas.Singleton.SetMapVoting(enable, fade);
    }

    [ClientRpc]
    public void RpcOnVotingEnd(string message)
    {
        EverywhereCanvas.Singleton.OnVotingEnd(message);
    }

    [ClientRpc]
    public void RpcPlayVotingSound(bool start)
    {
        AudioClip targetSound = start ? _votingStart : _votingEnd;

        SoundSystem.PlayInterfaceSound(new SoundTransporter(targetSound), volume: 0.4f);
    }

    [Command(requiresAuthority = false)]
    public void CmdVoteMap(string mapName)
    {
        Debug.Log($"Someone voted for {mapName}");

        GameLoop.Singleton.AddMapVote(mapName);
    }

    [ClientRpc]
    public void RpcForceClientsForLeaderboardUpdate()
    {
        Leaderboard.Singleton.UpdateLeaderboard();
    }

    [ClientRpc]
    public void RpcOnMatchStarted()
    {
        EverywhereCanvas.Singleton.FadeUIGameState(CanvasGameState.Game);
    }

    [Command(requiresAuthority = false)]
    public void CmdSendChatMessage(string message)
    {
        RpcReceiveChatMessage(message);
    }

    [ClientRpc]
    private void RpcReceiveChatMessage(string message)
    {
        Chat.Singleton.AddMessage(message);
    }

    [ClientRpc]
    public void RpcOnMatchEnd()
    {
        NetworkPlayer player = NetworkPlayer.LocalPlayer;

        ResultsStatsJobs.StatsToDisplay["Place"].Value = player.Place;
        ResultsStatsJobs.StatsToDisplay["Score"].Value = player.Score;
        ResultsStatsJobs.StatsToDisplay["Activity"].Value = player.Activity;
        ResultsStatsJobs.StatsToDisplay["Kills"].Value = player.Kills;
        ResultsStatsJobs.StatsToDisplay["Hits"].Value = player.Hits;
        ResultsStatsJobs.StatsToDisplay["Deaths"].Value = player.Deaths;
        ResultsStatsJobs.StatsToDisplay["Traumas"].Value = player.Traumas;

        int total = Math.Clamp((player.Score * 3) + player.Activity + (player.Kills * 6) + (player.Hits * 3) - (player.Deaths * 2) - (player.Traumas) - (player.Place * 5), 0, 1000);

        for (int i = RankStat.Rankings.Count - 1; i >= 0; i--)
        {
            if (total >= RankStat.Rankings[i].value)
            {
                ResultsStatsJobs.StatsToDisplay["Rank"].Value = RankStat.Rankings[i].key;
                break;
            }
        }
    }
}

public static class Extensions
{
    public static string ToSentence(this string Input)
    {
        return new string(Input.SelectMany((c, i) => i > 0 && char.IsUpper(c) ? new[] { ' ', c } : new[] { c }).ToArray());
    }
}
