using UnityEngine;

/// <summary>
/// Mobil cihazlardaki çentik (notch) ve güvenli alanları (safe area) yönetir.
/// Bu scripti Canvas'ın hemen altındaki tam ekran bir Panel'e ekleyin.
/// Diğer tüm UI elemanlarını bu Panel'in içine koyun.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    RectTransform panel;
    Rect lastSafeArea = new Rect(0, 0, 0, 0);
    Vector2Int lastScreenSize = new Vector2Int(0, 0);
    ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    void Awake()
    {
        panel = GetComponent<RectTransform>();
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        Rect safeArea = Screen.safeArea;

        if (safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y || Screen.orientation != lastOrientation)
        {
            lastScreenSize.x = Screen.width;
            lastScreenSize.y = Screen.height;
            lastOrientation = Screen.orientation;
            lastSafeArea = safeArea;

            ApplySafeArea(safeArea);
        }
    }

    void ApplySafeArea(Rect r)
    {
        // Güvenli alanı yoksaymak isterseniz burayı kapatabilirsiniz
        // r = new Rect(0, 0, Screen.width, Screen.height);

        Vector2 anchorMin = r.position;
        Vector2 anchorMax = r.position + r.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        if (panel != null)
        {
            panel.anchorMin = anchorMin;
            panel.anchorMax = anchorMax;
        }

        Debug.LogFormat("SafeArea uygulandı: x={0}, y={1}, w={2}, h={3} on full screen {4}x{5}",
            r.x, r.y, r.width, r.height, Screen.width, Screen.height);
    }
}
