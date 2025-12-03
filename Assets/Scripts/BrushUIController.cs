using UnityEngine;
using UnityEngine.UI;

public class BrushUIController : MonoBehaviour
{
    [Header("Referanslar")]
    public DirtCleaner dirtCleaner;     // Sahnedeki DirtCleaner
    public Image brushFollowImage;      // Ýmleci takip eden fýrça image
    public Button brushButton;          // Sað alttaki fýrça butonu

    [Header("Fýrça Spriteleri (0 = default, 1 = market fýrçasý vs.)")]
    public Sprite[] brushSprites;

    bool equipped = false;
    int currentBrushId = 0;

    void Start()
    {
        // Butona týklayýnca fýrçayý aç/kapa
        brushButton.onClick.AddListener(ToggleBrush);

        // Baþta takip eden fýrça görünmesin
        brushFollowImage.gameObject.SetActive(false);
        brushFollowImage.raycastTarget = false;

        // PLAYERPREFS'TEN BÝLGÝYÝ ÇEK
        currentBrushId = PlayerPrefs.GetInt("CurrentBrushId", 0);          // default 0
        equipped = PlayerPrefs.GetInt("BrushEquipped", 0) == 1;      // default kapalý

        // Seçili fýrçaya göre sprite'larý ayarla
        ApplyCurrentBrushVisuals();

        // Eðer kayýtlý durumda eldeyse, sahne açýldýðýnda da elde olsun
        if (equipped)
        {
            brushFollowImage.gameObject.SetActive(true);

            if (dirtCleaner != null)
                dirtCleaner.SetBrushEquipped(true);
        }
    }

    void Update()
    {
        if (!equipped) return;

        Vector3 pos;

        if (Input.touchCount > 0)
            pos = Input.touches[0].position;
        else
            pos = Input.mousePosition;

        brushFollowImage.rectTransform.position = pos;
    }

    // Þu anki brushId'ye göre buton ve takip eden sprite'ý ayarla
    void ApplyCurrentBrushVisuals()
    {
        if (brushSprites == null || brushSprites.Length == 0)
            return;

        if (currentBrushId < 0 || currentBrushId >= brushSprites.Length)
            currentBrushId = 0; // güvenlik için default

        Sprite s = brushSprites[currentBrushId];

        // Butondaki image
        Image btnImg = brushButton.GetComponent<Image>();
        if (btnImg != null)
            btnImg.sprite = s;

        // Takip eden image
        if (brushFollowImage != null)
            brushFollowImage.sprite = s;
    }

    // Ýleride istersen koddan da fýrça deðiþtirebilirsin
    public void ChangeBrush(int newBrushId)
    {
        currentBrushId = newBrushId;
        PlayerPrefs.SetInt("CurrentBrushId", newBrushId);
        PlayerPrefs.Save();

        ApplyCurrentBrushVisuals();
    }

    void ToggleBrush()
    {
        equipped = !equipped;

        // Temizleme sistemini tetikle
        if (dirtCleaner != null)
            dirtCleaner.SetBrushEquipped(equipped);

        // Takip eden fýrça açýk/kapalý
        brushFollowImage.gameObject.SetActive(equipped);

        // Durumu PlayerPrefs'te sakla
        PlayerPrefs.SetInt("BrushEquipped", equipped ? 1 : 0);
        PlayerPrefs.Save();
    }
}
