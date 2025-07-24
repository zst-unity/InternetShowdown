using Game.Core.Events;
using Game.Events.Gameplay;
using Game.Gameplay;
using TMPro;
using UnityEngine;

public class TempStateText : MonoBehaviour
{
    GameState state;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventBus<OnGameStateChange>.Listen((data) => state = data.state);
    }

    // Update is called once per frame
    void Update()
    {
        var type = state.isMatch ? "Match" : "Break";
        GetComponent<TMP_Text>().text = $"{type} {state.SecondsSinceTimerStarted} seconds";
    }
}
