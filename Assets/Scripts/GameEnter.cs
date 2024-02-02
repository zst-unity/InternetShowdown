using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEnter : MonoBehaviour
{
    private void Start()
    {
        Invoke(nameof(LoadLobby), 0.1f);
    }

    private void LoadLobby()
    {
        SceneManager.LoadScene("Menu", LoadSceneMode.Single);
    }
}
