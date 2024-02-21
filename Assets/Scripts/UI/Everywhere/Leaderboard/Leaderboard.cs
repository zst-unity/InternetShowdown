using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class Leaderboard : MonoBehaviour, IEverywhereCanvas
{
    public static Leaderboard Singleton { get; private set; }

    [SerializeField] private Place _placePrefab;
    [SerializeField] private Transform _placeContainer;

    private List<(string nickname, int score, int activity)> _leaderboard = new List<(string, int, int)>();

    public bool Active { get; set; }

    public void ResetCanvas()
    {
        Singleton = this;

        _leaderboard.Clear();
    }

    public void StartLeaderboard()
    {
        StartCoroutine(nameof(LeaderboardTickLoop));
    }

    private IEnumerator LeaderboardTickLoop()
    {
        while (NetworkClient.isConnected)
        {
            UpdateLeaderboard();

            yield return new WaitForSeconds(5f);
        }
    }

    public void UpdateLeaderboard()
    {
        List<(string nickname, int score, int activity)> newLeaderboardValue = new();

        List<NetworkPlayer> allPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();

        allPlayers.Sort((first, second) =>
        {
            if (first.Score == second.Score)
            {
                if (first.Activity < second.Activity) return 1;
                else return -1;
            }
            else if (first.Score < second.Score) return 1;
            else return -1;
        });

        int place = 1;
        foreach (NetworkPlayer connPlayer in allPlayers)
        {
            connPlayer.Place = place;
            newLeaderboardValue.Add(($"<color={connPlayer.ColorHEX}>{connPlayer.Nickname}</color>", connPlayer.Score, connPlayer.Activity));

            place++;
        }

        _leaderboard = newLeaderboardValue;

        UpdateLeaderboardUI();
    }

    private void UpdateLeaderboardUI()
    {
        ClearLeaderboardUI();
        int clampedLeaderboardSize = Mathf.Clamp(_leaderboard.Count, 0, 5);

        for (int idx = 0; idx < clampedLeaderboardSize; idx++)
        {
            int place = idx + 1;
            Place placeComp = Instantiate(_placePrefab.gameObject, _placeContainer).GetComponent<Place>();

            placeComp.Number.color = place switch
            {
                1 => (Color)ColorISH.Gold,
                2 => (Color)ColorISH.Silver,
                3 => (Color)ColorISH.Bronze,
                _ => Color.white,
            };
            placeComp.Number.text = place.ToString();
            placeComp.Nickname.text = _leaderboard[idx].nickname;
            placeComp.Score.text = _leaderboard[idx].score.ToString();
        }
    }

    private void ClearLeaderboardUI()
    {
        foreach (Transform place in _placeContainer)
        {
            Destroy(place.gameObject);
        }
    }

    public void OnDisconnect()
    {
        StopCoroutine(nameof(LeaderboardTickLoop));

        ClearLeaderboardUI();
    }
}
