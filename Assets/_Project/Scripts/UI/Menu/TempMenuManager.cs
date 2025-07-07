using UnityEngine;

public class TempMenuManager : MonoBehaviour
{
    public void SetSens(float value)
    {
        PlayerPrefs.SetFloat("sens", value);
    }
}