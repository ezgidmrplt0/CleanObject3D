using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenMarketButton : MonoBehaviour
{
    [Tooltip("Market sahnesinin adý (Build Settings’te yazan isim)")]
    public string marketSceneName = "MarketScene"; // kendi market sahnenin adýný yaz

    public void OpenMarket()
    {
        SceneManager.LoadScene(marketSceneName);
    }
}
