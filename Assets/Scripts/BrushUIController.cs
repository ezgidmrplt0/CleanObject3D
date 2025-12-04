using UnityEngine;
using UnityEngine.UI;

public class BrushUIController : MonoBehaviour
{
    [Header("Referanslar")]
    public DirtCleaner dirtCleaner;     // Sahnedeki DirtCleaner
    public CleanModeCamera cleanModeCamera; // Kamera kontrolü
    public Image brushFollowImage;      // İmleci takip eden fırça image
    public Button brushButton;          // Sağ alttaki fırça butonu

    [Header("Buton Spriteleri")]
    [Tooltip("Kamera modunda (fırça kapalı) gösterilecek sprite")]
    public Sprite cameraModeSprite;
    [Tooltip("Temizlik modunda (fırça açık) gösterilecek sprite")]
    public Sprite cleanModeSprite;

    [Header("Fırça Spriteleri (takip eden fırça için)")]
    public Sprite[] brushSprites;

    bool equipped = false;
    int currentBrushId = 0;
    Image buttonImage;

    void Start()
    {
        // Butona tıklayınca fırçayı aç/kapa
        if (brushButton != null)
        {
            brushButton.onClick.AddListener(ToggleBrush);
            buttonImage = brushButton.GetComponent<Image>();
        }

        // Başta takip eden fırça görünmesin
        if (brushFollowImage != null)
        {
            brushFollowImage.gameObject.SetActive(false);
            brushFollowImage.raycastTarget = false;
        }

        // Fırça sprite ID'sini al
        currentBrushId = PlayerPrefs.GetInt("CurrentBrushId", 0);
        
        // HER ZAMAN KAPALI BAŞLA
        equipped = false;

        // CleanModeCamera'yı otomatik bul
        if (cleanModeCamera == null)
            cleanModeCamera = FindObjectOfType<CleanModeCamera>();

        // Takip eden fırça sprite'ını ayarla
        ApplyBrushFollowVisual();
        
        // Buton sprite'ını ayarla - başlangıçta kamera modu
        UpdateButtonSprite();

        // DirtCleaner'a fırça kapalı bilgisini ver
        if (dirtCleaner != null)
            dirtCleaner.SetBrushEquipped(false);
    }

    void Update()
    {
        if (!equipped) return;
        if (brushFollowImage == null) return;

        Vector3 pos;

        if (Input.touchCount > 0)
            pos = Input.touches[0].position;
        else
            pos = Input.mousePosition;

        brushFollowImage.rectTransform.position = pos;
    }

    // Şu anki brushId'ye göre takip eden fırça sprite'ını ayarla
    void ApplyBrushFollowVisual()
    {
        if (brushFollowImage == null) return;
        if (brushSprites == null || brushSprites.Length == 0) return;

        if (currentBrushId < 0 || currentBrushId >= brushSprites.Length)
            currentBrushId = 0;

        brushFollowImage.sprite = brushSprites[currentBrushId];
    }

    // Buton sprite'ını moda göre güncelle
    void UpdateButtonSprite()
    {
        if (buttonImage == null) return;

        if (equipped)
        {
            // Temizlik modu - cleanModeSprite göster
            if (cleanModeSprite != null)
                buttonImage.sprite = cleanModeSprite;
        }
        else
        {
            // Kamera modu - cameraModeSprite göster
            if (cameraModeSprite != null)
                buttonImage.sprite = cameraModeSprite;
        }
    }

    void ToggleBrush()
    {
        equipped = !equipped;

        // Temizleme sistemini tetikle
        if (dirtCleaner != null)
            dirtCleaner.SetBrushEquipped(equipped);

        // Kamerayı kilitle/aç - fırça eldeyken kamera kilitli olmalı
        if (cleanModeCamera != null)
            cleanModeCamera.controlsEnabled = !equipped;

        // Takip eden fırça açık/kapalı
        if (brushFollowImage != null)
            brushFollowImage.gameObject.SetActive(equipped);

        // Buton sprite'ını güncelle
        UpdateButtonSprite();
    }

    // İleride koddan fırça değiştirmek için
    public void ChangeBrush(int newBrushId)
    {
        currentBrushId = newBrushId;
        PlayerPrefs.SetInt("CurrentBrushId", newBrushId);
        PlayerPrefs.Save();
        ApplyBrushFollowVisual();
    }
}
