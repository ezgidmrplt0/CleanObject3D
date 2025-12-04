using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Market panelini yöneten script.
/// Ayrı sahne yerine panel olarak çalışır.
/// </summary>
public class MarketPanelController : MonoBehaviour
{
    [Header("Panel Referansları")]
    [Tooltip("Market paneli (açılıp kapanacak olan GameObject)")]
    public GameObject marketPanel;

    [Header("UI Referansları")]
    [Tooltip("Toplam coin miktarını gösteren text")]
    public TextMeshProUGUI coinsText;

    [Header("Oyun Referansları")]
    [Tooltip("BrushUIController - fırça değişimi için")]
    public BrushUIController brushUIController;

    void Start()
    {
        // Başlangıçta panel kapalı olsun
        if (marketPanel != null)
            marketPanel.SetActive(false);

        UpdateCoinsUI();
    }

    /// <summary>
    /// Market panelini açar
    /// </summary>
    public void OpenMarket()
    {
        if (marketPanel != null)
        {
            marketPanel.SetActive(true);
            UpdateCoinsUI();
            
            // Oyunu duraklat (opsiyonel)
            Time.timeScale = 0f;
        }
    }

    /// <summary>
    /// Market panelini kapatır
    /// </summary>
    public void CloseMarket()
    {
        if (marketPanel != null)
        {
            marketPanel.SetActive(false);
            
            // Oyunu devam ettir
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// Market panelini aç/kapat toggle
    /// </summary>
    public void ToggleMarket()
    {
        if (marketPanel == null) return;

        if (marketPanel.activeSelf)
            CloseMarket();
        else
            OpenMarket();
    }

    /// <summary>
    /// Coin UI'ını günceller
    /// </summary>
    public void UpdateCoinsUI()
    {
        if (coinsText == null) return;
        int currentCoins = PlayerPrefs.GetInt("Coins", 0);
        coinsText.text = currentCoins.ToString();
    }

    /// <summary>
    /// Fırça satın alındığında çağrılır
    /// </summary>
    public void OnBrushPurchased(int brushId)
    {
        // BrushUIController'a yeni fırçayı bildir
        if (brushUIController != null)
            brushUIController.ChangeBrush(brushId);

        UpdateCoinsUI();
    }
}
