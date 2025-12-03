using UnityEngine;
using UnityEngine.UI;

public class BrushUIController : MonoBehaviour
{
    [Header("Referanslar")]
    public DirtCleaner dirtCleaner;     // Sahnedeki DirtCleaner
    public CleanModeCamera cleanModeCamera; // Kamera kontrolü
    public Image brushFollowImage;      // İmleci takip eden fırça image
    public Button brushButton;          // Sağ alttaki fırça butonu

    [Header("Fırça Spriteleri (0 = default, 1 = market fırçası vs.)")]
    public Sprite[] brushSprites;

    bool equipped = false;
    int currentBrushId = 0;

    void Start()
    {
        // Butona tıklayınca fırçayı aç/kapa
        if (brushButton != null)
            brushButton.onClick.AddListener(ToggleBrush);

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

        // Seçili fırçaya göre sprite'ları ayarla
        ApplyCurrentBrushVisuals();

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

    // Şu anki brushId'ye göre buton ve takip eden sprite'ı ayarla
    void ApplyCurrentBrushVisuals()
    {
        if (brushSprites == null || brushSprites.Length == 0)
            return;

        if (currentBrushId < 0 || currentBrushId >= brushSprites.Length)
            currentBrushId = 0;

        Sprite s = brushSprites[currentBrushId];

        // Butondaki image
        if (brushButton != null)
        {
            Image btnImg = brushButton.GetComponent<Image>();
            if (btnImg != null)
                btnImg.sprite = s;
        }

        // Takip eden image
        if (brushFollowImage != null)
            brushFollowImage.sprite = s;
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
    }

    // İleride koddan fırça değiştirmek için
    public void ChangeBrush(int newBrushId)
    {
        currentBrushId = newBrushId;
        PlayerPrefs.SetInt("CurrentBrushId", newBrushId);
        PlayerPrefs.Save();
        ApplyCurrentBrushVisuals();
    }
}
