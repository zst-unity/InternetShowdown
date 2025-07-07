using UnityEngine;

public class TempMenuManager : MonoBehaviour
{
    private void Awake()
    {
        PlayerPrefs.SetFloat("sens", 1f);
    }

    public void SetSens(float value)
    {
        PlayerPrefs.SetFloat("sens", value);
    }
}