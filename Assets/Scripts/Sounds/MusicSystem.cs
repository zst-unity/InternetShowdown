using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class MusicSystem : MonoBehaviour
{
    [SerializeField] private List<Music> _musics;
    public static ActiveMusic MainMusic { get; private set; }

    public static MusicSystem Singleton { get; private set; }

    private void OnDestroy()
    {
        Singleton = null;
        MainMusic = null;
    }

    private void Awake()
    {
        Singleton = this;
    }

    public static int GetRandomMusicIndex()
    {
        var sceneMusics = Singleton._musics.FindAll(music => music.scene == SceneManager.GetActiveScene().buildIndex);
        if (sceneMusics.Count == 0)
        {
            Debug.LogWarning("Cant find musics for this scene");
            return -1;
        }

        return Singleton._musics.IndexOf(sceneMusics[Random.Range(0, sceneMusics.Count)]);
    }

    public static void StartMusic(int index, float offset = 0f)
    {
        if (index == -1)
        {
            MainMusic = null;
            return;
        }

        AudioClip target = Singleton._musics[index].music;
        AudioSource source = SoundSystem.PlaySound(new SoundTransporter(target), new SoundPositioner(Vector3.zero), SoundType.Music, volume: 0.525f, enableFade: false);
        source.time = offset;

        ActiveMusic newInstance = new()
        {
            Current = target,
            Source = source
        };

        MainMusic = newInstance;
    }

    private void Update()
    {
        if (MainMusic != null && MainMusic.Source && GameInfo.Singleton.CurrentMusicOffset > 0)
        {
            var speed = 1f + (GameInfo.Singleton.CurrentMusicOffset - MainMusic.Source.time);

            if (speed <= 0.25f || speed >= 1.75f) MainMusic.Source.time = GameInfo.Singleton.CurrentMusicOffset;
            else MainMusic.Source.pitch = Mathf.Lerp(MainMusic.Source.pitch, speed, Time.deltaTime * 5);
        }
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
