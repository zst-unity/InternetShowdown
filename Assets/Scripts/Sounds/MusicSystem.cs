using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class MusicSystem : MonoBehaviour
{
    [SerializeField] private List<Music> _musics;

    public static ActiveMusic MainSoundtrack { get; private set; }

    public static MusicSystem Singleton()
    {
        return FindFirstObjectByType<MusicSystem>(FindObjectsInactive.Include);
    }

    public static int GetRandomMusicIndex()
    {
        MusicSystem current = Singleton();

        var sceneMusics = current._musics.FindAll(music => music.scene == SceneManager.GetActiveScene().buildIndex);
        if (sceneMusics.Count == 0)
        {
            Debug.LogWarning("Cant find musics for this scene");
            return -1;
        }

        return current._musics.IndexOf(sceneMusics[Random.Range(0, sceneMusics.Count)]);
    }

    public static ActiveMusic StartMusic(MusicGameState state, int index, float offset = 0f)
    {
        if (index == -1) return null;
        MusicSystem current = Singleton();

        AudioClip target = current._musics[index].music;
        AudioSource source = SoundSystem.PlaySound(new SoundTransporter(target), new SoundPositioner(Vector3.zero), SoundType.Music, volume: 0.525f, enableFade: false);
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

[Serializable]
public class Music
{
    [Scene] public int scene;
    public AudioClip music;
}

public enum MusicGameState
{
    Lobby,
    Match
}
