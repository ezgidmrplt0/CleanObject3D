using UnityEngine;
using UnityEngine.SceneManagement;

public class MarketBrushButton : MonoBehaviour
{
    [Tooltip("Bu butonun temsil ettiði fýrça id'si. 0 = default, 1 = market fýrçasý, 2...")]
    public int brushId = 1;

    [Tooltip("Fýrçayý aldýktan sonra oyunun en baþtan baþlayacaðý sahne adý")]
    public string gameStartSceneName = "GameStartScene"; // BURAYA oyunun ilk sahnesinin adýný yaz

    public void OnBrushClicked()
    {
        // Seçilen fýrçayý kaydet
        PlayerPrefs.SetInt("CurrentBrushId", brushId);
        PlayerPrefs.SetInt("BrushEquipped", 1);  // istersen elde baþlasýn
        PlayerPrefs.Save();

        // OYUN ÝÇÝ ÝLERLEMEN VARSA BURADA SIFIRLAYABÝLÝRSÝN (opsiyonel)
        // Örneðin:
        // PlayerPrefs.DeleteKey("CurrentLevel");
        // PlayerPrefs.DeleteKey("Score");
        // vs...

        // Oyunu en baþtan baþlat: ilk sahneni yükle
        SceneManager.LoadScene(gameStartSceneName);
    }
}
