using UnityEngine;
using UnityEngine.UI;

public class BrushUIController : MonoBehaviour
{
    [Header("Referanslar")]
    public DirtCleaner dirtCleaner;     // Sahnedeki DirtCleaner
    public Image brushFollowImage;      // �mleci takip eden f�r�a image
    public Button brushButton;          // Sa� alttaki f�r�a butonu

    [Header("F�r�a Spriteleri (0 = default, 1 = market f�r�as� vs.)")]
    public Sprite[] brushSprites;

    bool equipped = false;
    int currentBrushId = 0;

    void Start()
    {
        // Butona t�klay�nca f�r�ay� a�/kapa
        brushButton.onClick.AddListener(ToggleBrush);

        // Ba�ta takip eden f�r�a g�r�nmesin
        brushFollowImage.gameObject.SetActive(false);
        brushFollowImage.raycastTarget = false;

        // PLAYERPREFS'TEN B�LG�Y� �EK
        currentBrushId = PlayerPrefs.GetInt("CurrentBrushId", 0);          // default 0
        equipped = PlayerPrefs.GetInt("BrushEquipped", 0) == 1;      // default kapal�

        Debug.Log($"[BrushUI] Start: currentBrushId={currentBrushId}, equipped={equipped}");

        // Se�ili f�r�aya g�re sprite'lar� ayarla
        ApplyCurrentBrushVisuals();

        // E�er kay�tl� durumda eldeyse, sahne a��ld���nda da elde olsun
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

    // �u anki brushId'ye g�re buton ve takip eden sprite'� ayarla
    void ApplyCurrentBrushVisuals()
    {
        if (brushSprites == null || brushSprites.Length == 0)
            return;

        if (currentBrushId < 0 || currentBrushId >= brushSprites.Length)
            currentBrushId = 0; // g�venlik i�in default

        Sprite s = brushSprites[currentBrushId];

        // Butondaki image
        Image btnImg = brushButton.GetComponent<Image>();
        if (btnImg != null)
            btnImg.sprite = s;

        // Takip eden image
        if (brushFollowImage != null)
            brushFollowImage.sprite = s;
    }

    // �leride istersen koddan da f�r�a de�i�tirebilirsin
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

        Debug.Log($"[BrushUI] ToggleBrush: equipped={equipped}");

        // Temizleme sistemini tetikle
        if (dirtCleaner != null)
        {
            Debug.Log("[BrushUI] ToggleBrush: DirtCleaner'e SetBrushEquipped gönderiliyor");
            dirtCleaner.SetBrushEquipped(equipped);
        }
        else
        {
            Debug.LogWarning("[BrushUI] ToggleBrush: dirtCleaner REFERANSI YOK!");
        }

        // Takip eden f�r�a a��k/kapal�
        brushFollowImage.gameObject.SetActive(equipped);

        // Durumu PlayerPrefs'te sakla
        PlayerPrefs.SetInt("BrushEquipped", equipped ? 1 : 0);
        PlayerPrefs.Save();
    }
}
