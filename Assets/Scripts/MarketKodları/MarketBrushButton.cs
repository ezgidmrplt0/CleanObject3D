using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Market panelindeki ürün satırlarını yönetir.
/// Her ürün için bir tane bu script eklenir.
/// </summary>
public class MarketBrushButton : MonoBehaviour
{
    [Header("Ürün Bilgileri")]
    [Tooltip("Bu butonun temsil ettiği fırça id'si. 0 = default, 1, 2...")]
    public int brushId = 1;

    [Tooltip("Bu ürünün fiyatı (Coins cinsinden)")]
    public int price = 50;

    [Header("UI Referansları")]
    [Tooltip("Sağdaki ana buton (BUY/EQUIP/EQUIPPED)")]
    public Button actionButton;

    [Tooltip("Buton üzerindeki text")]
    public TextMeshProUGUI buttonText;

    [Tooltip("Fiyat text'i (ürün bilgisi kısmında)")]
    public TextMeshProUGUI priceText;

    [Header("Buton Renkleri")]
    public Color buyColor = new Color(0.8f, 0.2f, 0.5f);      // Pembe - BUY
    public Color equipColor = new Color(0.2f, 0.7f, 0.3f);    // Yeşil - EQUIP
    public Color equippedColor = new Color(0.5f, 0.5f, 0.5f); // Gri - EQUIPPED

    [Header("Panel Controller")]
    public MarketPanelController marketController;

    string unlockKey;
    Image buttonImage;

    void Start()
    {
        unlockKey = "BrushUnlocked_" + brushId;

        if (actionButton != null)
        {
            buttonImage = actionButton.GetComponent<Image>();
            actionButton.onClick.AddListener(OnButtonClicked);
        }

        if (marketController == null)
            marketController = FindObjectOfType<MarketPanelController>();

        // Fiyat text'ini ayarla
        if (priceText != null)
            priceText.text = "+" + price + " Gold";

        UpdateVisuals();
    }

    void OnEnable()
    {
        // Biraz gecikme ile güncelle - UI hazır olsun
        Invoke("UpdateVisuals", 0.05f);
    }

    /// <summary>
    /// Buton görsellerini duruma göre günceller
    /// </summary>
    public void UpdateVisuals()
    {
        if (actionButton == null) return;
        if (buttonImage == null) buttonImage = actionButton.GetComponent<Image>();

        bool isUnlocked = IsBrushUnlocked();
        int currentEquippedId = PlayerPrefs.GetInt("CurrentBrushId", 0);
        bool isEquipped = (currentEquippedId == brushId);

        if (!isUnlocked)
        {
            // Satın alınmamış - BUY göster
            if (buttonText != null) buttonText.text = "BUY";
            if (buttonImage != null) buttonImage.color = buyColor;
            actionButton.interactable = true;
        }
        else if (isEquipped)
        {
            // Satın alınmış VE şu an takılı - EQUIPPED göster
            if (buttonText != null) buttonText.text = "EQUIPPED";
            if (buttonImage != null) buttonImage.color = equippedColor;
            actionButton.interactable = false;
        }
        else
        {
            // Satın alınmış AMA takılı değil - EQUIP göster
            if (buttonText != null) buttonText.text = "EQUIP";
            if (buttonImage != null) buttonImage.color = equipColor;
            actionButton.interactable = true;
        }
    }

    /// <summary>
    /// Butona tıklandığında
    /// </summary>
    public void OnButtonClicked()
    {
        if (IsBrushUnlocked())
        {
            // Zaten satın alınmış - sadece equip et
            EquipBrush();
        }
        else
        {
            // Satın alma işlemi
            TryPurchase();
        }
    }

    void TryPurchase()
    {
        int currentCoins = PlayerPrefs.GetInt("Coins", 0);

        if (currentCoins < price)
        {
            Debug.Log("Yetersiz para! Gerekli: " + price + " - Mevcut: " + currentCoins);
            // TODO: Yetersiz para uyarısı göster
            return;
        }

        // Para düş
        currentCoins -= price;
        PlayerPrefs.SetInt("Coins", currentCoins);

        // Ürünü unlock et
        PlayerPrefs.SetInt(unlockKey, 1);
        PlayerPrefs.Save();

        // Equip et
        EquipBrush();

        Debug.Log("Fırça satın alındı! ID: " + brushId);
    }

    void EquipBrush()
    {
        PlayerPrefs.SetInt("CurrentBrushId", brushId);
        PlayerPrefs.Save();

        // MarketPanelController'a bildir
        if (marketController != null)
        {
            marketController.OnBrushPurchased(brushId);
            marketController.UpdateCoinsUI();
        }

        // Tüm butonların görsellerini güncelle
        RefreshAllButtons();
    }

    void RefreshAllButtons()
    {
        MarketBrushButton[] allButtons = FindObjectsOfType<MarketBrushButton>();
        foreach (var btn in allButtons)
        {
            btn.UpdateVisuals();
        }
    }

    bool IsBrushUnlocked()
    {
        // brushId 0 (default fırça) her zaman açık
        if (brushId == 0) return true;
        return PlayerPrefs.GetInt(unlockKey, 0) == 1;
    }
}
