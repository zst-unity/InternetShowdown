using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundSettings : MonoBehaviour
{
    private const string MASTER_NAME = "Master";
    private const string SFX_NAME = "SFX";
    private const string UI_NAME = "UI";
    private const string MUSIC_NAME = "Music";

    [SerializeField] private AudioMixer _targetMixer;

    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _uiSlider;
    [SerializeField] private Slider _musicSlider;

    private void Start()
    {
        LoadMaster();
        LoadSFX();
        LoadUI();
        LoadMusic();
    }

    public void LoadMaster()
    {
        _masterSlider.value = DbToAmp(LoadMixerGroup(MASTER_NAME));
    }

    public void LoadSFX()
    {
        _sfxSlider.value = DbToAmp(LoadMixerGroup(SFX_NAME));
    }

    public void LoadUI()
    {
        _uiSlider.value = DbToAmp(LoadMixerGroup(UI_NAME));
    }

    public void LoadMusic()
    {
        _musicSlider.value = DbToAmp(LoadMixerGroup(MUSIC_NAME));
    }

    private float AmpToDb(float amp)
    {
        if (amp <= 0.001f)
        {
            return -80f;
        }

        return 20 * Mathf.Log10(amp);
    }

    private float DbToAmp(float db)
    {
        return Mathf.Pow(10, db / 20);
    }

    public void SetMaster(float value)
    {
        SetMixerGroup(MASTER_NAME, AmpToDb(value));
    }

    public void SetSFX(float value)
    {
        SetMixerGroup(SFX_NAME, AmpToDb(value));
    }

    public void SetUI(float value)
    {
        SetMixerGroup(UI_NAME, AmpToDb(value));
    }

    public void SetMusic(float value)
    {
        SetMixerGroup(MUSIC_NAME, AmpToDb(value));
    }

    private void SetMixerGroup(string name, float value)
    {
        _targetMixer.SetFloat(name, value);
        PlayerPrefs.SetFloat(name, value);
    }

    private float LoadMixerGroup(string name)
    {
        float value = PlayerPrefs.GetFloat(name, 0);

        _targetMixer.SetFloat(name, value);
        return value;
    }
}
