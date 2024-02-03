using UnityEngine;
using UnityEngine.UI;

public class GameplaySettings : MonoBehaviour
{
    [SerializeField] private Slider _sensitivitySlider;

    private void Start()
    {
        LoadSensitivity();
    }

    public void LoadSensitivity()
    {
        _sensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", 2);
    }

    public void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat("Sensitivity", value);
        var camera = FindFirstObjectByType<CameraMovement>();
        if (camera) camera.SetSensitivity(value);
    }
}
