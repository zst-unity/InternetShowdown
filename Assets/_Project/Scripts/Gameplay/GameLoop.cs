using System;
using Game.Core.Events;
using Game.Core.Maps;
using Game.Events.Gameplay;
using Game.Events.ItemSpawner;
using Game.Network.Messages;
using Mirror;
using UnityEngine;

namespace Game.Gameplay
{
    [Serializable]
    public struct GameState
    {
        public bool isMatch;
        public long timerBeginTicks;
        public int mapIndex;
        public int soundtrackIndex;

        // why tf are getters can be made readonly
        public readonly float SecondsSinceTimerStarted => (float)new TimeSpan(DateTime.Now.Ticks - timerBeginTicks).TotalSeconds;

        public GameState(bool isMatch, int mapIndex, int soundtrackIndex)
        {
            this.isMatch = isMatch;
            this.mapIndex = mapIndex;
            this.soundtrackIndex = soundtrackIndex;
            timerBeginTicks = DateTime.Now.Ticks;
        }

        public readonly override string ToString()
        {
            return $"isMatch: {isMatch} | ticks: {timerBeginTicks} | map: {mapIndex} | ost: {soundtrackIndex}";
        }
    }

    public class GameLoop : NetworkBehaviour
    {
        public float breakDuration;
        public float roundDuration;

        [SyncVar(hook = nameof(OnStateChanged)), ReadOnly] public GameState state;

        private void OnStateChanged(GameState old, GameState _new)
        {
            EventBus<OnGameStateChange>.Invoke(new() { state = _new });
        }

        private void Start()
        {
            if (!isServer) return;
            state = new(false, -1, -1);
        }

        private void Update()
        {
            if (!isServer) return;

            if (!state.isMatch && state.SecondsSinceTimerStarted >= breakDuration)
            {
                var idx = UnityEngine.Random.Range(0, MapPool.maps.Length);
                var conf = MapPool.maps[idx];
                MapLoader.Load(conf);
                EventBus<SetItemSpawnerActive>.Invoke(new() { active = true });

                state = new(true, idx, UnityEngine.Random.Range(0, conf.soundtracks.Length));
            }
            else if (state.isMatch && state.SecondsSinceTimerStarted >= roundDuration)
            {
                EventBus<SetItemSpawnerActive>.Invoke(new() { active = false });
                MapLoader.Unload();

                state = new(false, -1, -1);
            }
        }
    }
}