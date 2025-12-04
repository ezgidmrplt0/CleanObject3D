using UnityEngine;

/// <summary>
/// Market panelini açan buton.
/// Artık sahne değiştirmek yerine panel açıyor.
/// </summary>
public class OpenMarketButton : MonoBehaviour
{
    [Tooltip("MarketPanelController referansı")]
    public MarketPanelController marketController;

    void Start()
    {
        // Otomatik bul
        if (marketController == null)
            marketController = FindObjectOfType<MarketPanelController>();
    }

    /// <summary>
    /// Market panelini açar
    /// </summary>
    public void OpenMarket()
    {
        if (marketController != null)
            marketController.OpenMarket();
        else
            Debug.LogWarning("MarketPanelController bulunamadı!");
    }
}
