using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MarketBrushButton : MonoBehaviour
{
    [Header("Bu butonun temsil ettiði fýrça")]
    [Tooltip("Bu butonun temsil ettiði fýrça id'si. 0 = default, 1 = market fýrçasý, 2...")]
    public int brushId = 1;

    [Header("Fiyat Ayarlarý")]
    [Tooltip("Bu fýrçanýn fiyatý (Coins cinsinden)")]
    public int price = 50;

    [Tooltip("Fiyatý gösterdiðin TextMeshPro (opsiyonel, inspector'dan baðla)")]
    public TextMeshProUGUI priceText;

    [Tooltip("Market sahnesinde toplam coin'i gösteren Text (opsiyonel)")]
    public TextMeshProUGUI coinsText;

    [Header("Satýn aldýktan sonra gidilecek sahne")]
    [Tooltip("Fýrçayý aldýktan sonra oyunun en baþtan baþlayacaðý sahne adý")]
    public string gameStartSceneName = "GameStartScene"; // BURAYA oyunun ilk sahnesinin adýný yaz

    string unlockKey; // BrushUnlocked_# key'i

    void Start()
    {
        unlockKey = "BrushUnlocked_" + brushId;

        // Fiyat text'ine otomatik yaz
        if (priceText != null)
        {
            priceText.text = price.ToString();
        }

        // Baþlangýçta coin UI güncelle
        UpdateCoinsUI();

        // Eðer bu fýrça önceden satýn alýnmýþsa UI'da istersen gösterim deðiþikliði yap
        if (IsBrushUnlocked())
        {
            // Örnek: fiyat text'ini "Satýn Alýndý" yapabilirsin
            if (priceText != null)
            {
                priceText.text = "Satýn Alýndý";
            }
        }
    }

    // Butonun OnClick'ine baðlanacak fonksiyon
    public void OnBrushClicked()
    {
        // Eðer zaten satýn alýndýysa sadece seç ve oyunu baþlat
        if (IsBrushUnlocked())
        {
            EquipBrushAndStartGame();
            return;
        }

        int currentCoins = PlayerPrefs.GetInt("Coins", 0);

        if (currentCoins < price)
        {
            Debug.Log("Yetersiz para! Gerekli: " + price + " - Senin: " + currentCoins);
            // Burada istersen ekrana "Yetersiz para" uyarý yazýsý gösterebilirsin.
            return;
        }

        // Yeterli para var -> coins düþ
        currentCoins -= price;
        PlayerPrefs.SetInt("Coins", currentCoins);

        // Bu fýrçayý unlock et
        PlayerPrefs.SetInt(unlockKey, 1);

        // Seçili fýrça yap
        PlayerPrefs.SetInt("CurrentBrushId", brushId);
        PlayerPrefs.SetInt("BrushEquipped", 1);

        PlayerPrefs.Save();

        // UI güncelle
        UpdateCoinsUI();
        if (priceText != null)
        {
            priceText.text = "Satýn Alýndý";
        }

        // Oyunu en baþtan baþlat
        EquipBrushAndStartGame();
    }

    bool IsBrushUnlocked()
    {
        return PlayerPrefs.GetInt(unlockKey, 0) == 1;
    }

    void EquipBrushAndStartGame()
    {
        // Burada istersen sadece sahneyi açmadan da kalabilirsin
        // ama þu an senin mantýðýna göre oyunu baþtan baþlatýyoruz
        SceneManager.LoadScene(gameStartSceneName);
    }

    void UpdateCoinsUI()
    {
        if (coinsText == null) return;

        int currentCoins = PlayerPrefs.GetInt("Coins", 0);
        coinsText.text = currentCoins.ToString();
    }
}
