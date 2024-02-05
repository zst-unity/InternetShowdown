using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManagerCrashHandler : MonoBehaviour
{
    private void Awake()
    {
        if (FindObjectsByType<NetworkManagerCrashHandler>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += (a, b) =>
            {
                if (NetworkManager.singleton == null)
                {
                    Debug.LogWarning("network manager crashed, restarting game");

                    var everywhereCanvas = EverywhereCanvas.Singleton.gameObject;
                    Destroy(everywhereCanvas);
                    SceneManager.LoadScene(0);
                }
            };
        }
    }
}
