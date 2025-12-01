using UnityEngine;
using UnityEngine.UI;

public class BrushUIController : MonoBehaviour
{
    [Header("Referanslar")]
    public DirtCleaner dirtCleaner;     // Sahnedeki DirtCleaner'ý buraya sürükle
    public Image brushFollowImage;      // Canvas'taki BrushFollow image
    public Button brushButton;          // BrushButton

    bool equipped = false;

    void Start()
    {
        // Butona týklandýðýnda çalýþacak fonksiyon
        brushButton.onClick.AddListener(ToggleBrush);

        // Baþta kapalý
        brushFollowImage.gameObject.SetActive(false);

        // BU SATIR ÖNEMLÝ: Brush image buton týklamasýný engellemesin
        brushFollowImage.raycastTarget = false;
    }

    void Update()
    {
        // Fýrça elde deðilse takip etme
        if (!equipped) return;

        Vector3 pos;

        // Dokunmatik varsa onu kullan
        if (Input.touchCount > 0)
        {
            pos = Input.touches[0].position;
        }
        else
        {
            pos = Input.mousePosition;
        }

        // UI resmi imleci takip etsin
        brushFollowImage.rectTransform.position = pos;
    }

    void ToggleBrush()
    {
        equipped = !equipped;

        // DirtCleaner'a haber ver
        if (dirtCleaner != null)
            dirtCleaner.SetBrushEquipped(equipped);

        // Fýrça resmi aç/kapa
        brushFollowImage.gameObject.SetActive(equipped);
    }
}
