using System.Collections.Generic;
using UnityEngine;

public class MusicSystem : MonoBehaviour
{
    [SerializeField] private List<AudioClip> _lobbySoundtracks;
    [SerializeField] private List<AudioClip> _matchSoundtracks;

    public static ActiveMusic MainSoundtrack { get; private set; }

    public static MusicSystem Singleton()
    {
        return FindFirstObjectByType<MusicSystem>(FindObjectsInactive.Include);
    }

    public static int? GetIndex(MusicGameState state)
    {
        MusicSystem current = Singleton();

        if (state == MusicGameState.Lobby)
        {
            if (current._lobbySoundtracks.Count != 0) return Random.Range(0, current._lobbySoundtracks.Count);
        }
        else if (state == MusicGameState.Match)
        {
            if (current._matchSoundtracks.Count != 0) return Random.Range(0, current._matchSoundtracks.Count);
        }

        return null;
    }

    public static ActiveMusic StartMusic(MusicGameState state, float offset = 0f, int? idx = null)
    {
        MusicSystem current = Singleton();

        AudioClip target = null;
        AudioSource source = null;

        if (state == MusicGameState.Lobby)
        {
            if (current._lobbySoundtracks.Count > 0) target = current._lobbySoundtracks[idx ?? Random.Range(0, current._lobbySoundtracks.Count)];
        }
        else if (state == MusicGameState.Match)
        {
            if (current._matchSoundtracks.Count > 0) target = current._matchSoundtracks[idx ?? Random.Range(0, current._matchSoundtracks.Count)];
        }

        if (target == null)
        {
            Debug.LogWarning("There's no music to play");
            return null;
        }

        source = SoundSystem.PlaySound(new SoundTransporter(target), new SoundPositioner(Vector3.zero), SoundType.Music, volume: 0.525f, enableFade: false);
        source.time = offset;

        ActiveMusic newInstance = new ActiveMusic()
        {
            Current = target,
            Source = source
        };

        MainSoundtrack = newInstance;

        return newInstance;
    }
}

public class ActiveMusic
{
    public AudioClip Current;
    public AudioSource Source;
}

public enum MusicGameState
{
    Lobby,
    Match
}
