using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MobileCameraController : MonoBehaviour
{
    [Header("Genel")]
    public Camera cam;
    public bool ignoreTouchesOverUI = true;

    [Header("Zoom (Pinch / Scroll)")]
    public float minOrthoSize = 3f;          // daha küçük = daha fazla yakın
    public float maxOrthoSize = 12f;
    [Range(0.01f, 1f)] public float zoomSpeed = 0.08f;   // pinch hız katsayısı
    public float zoomSmoothing = 0.15f;      // 0 = anlık, 0.15-0.2 = yumuşak
    public float scrollZoomStep = 1.0f;      // editörde scroll başına değişim

    [Header("Pan (Sürükleme)")]
    [Range(0.1f, 5f)] public float panSpeed = 1.0f;

    [Header("Sınırlar (Dünya)")]
    public Rect worldBounds = new Rect(-10, -10, 20, 20);

    [Header("Buton Kontrolü")]
    public bool controlsEnabled = false;     // Başlangıçta KAPALI (sabit)
    public TextMeshProUGUI stateLabel;       // (opsiyonel) buton üzeri yazı

    // Dahili
    float targetOrtho;
    Vector2 lastTouchPos;
    bool hadSingleTouchLastFrame;
    bool changedThisFrame = false; // o karede pan/zoom oldu mu?

    void Start()
    {
        if (!cam) cam = GetComponent<Camera>();
        cam.orthographic = true;

        // Mevcut değeri hedef yap – böylece açarken zıplama olmaz
        targetOrtho = cam.orthographicSize;

        UpdateLabel();
    }

    void Update()
    {
        // KAPALIYKEN: hiç işlem yapma → kamera sabit kalır
        if (!controlsEnabled) return;

        changedThisFrame = false; // kare başı sıfırla

#if UNITY_EDITOR || UNITY_STANDALONE
        MouseEmulation();  // editörde scroll + orta tuş pan
#endif
        HandleTouch();     // mobil dokunuşları/pinch

        // Sadece hedef değiştiyse zoom’u yumuşat
        if (Mathf.Abs(targetOrtho - cam.orthographicSize) > 0.0001f)
        {
            cam.orthographicSize = Mathf.Lerp(
                cam.orthographicSize, targetOrtho,
                1f - Mathf.Exp(-zoomSmoothing * 60f * Time.deltaTime)
            );
            changedThisFrame = true;
        }

        // Sadece gerçekten pan/zoom olduğunda clamp uygula
        if (changedThisFrame)
            ClampCameraToBounds();
    }

    // UI butonu bu fonksiyonu çağıracak
    public void ToggleControls()
    {
        controlsEnabled = !controlsEnabled;

        if (controlsEnabled)
        {
            // Açılırken mevcut değerleri hedef yap → hiç kıpırdama
            targetOrtho = cam.orthographicSize;
            hadSingleTouchLastFrame = false;
        }

        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (stateLabel)
            stateLabel.text = controlsEnabled ? "Kamera: AÇIK" : "Kamera: KAPALI";
    }

    void HandleTouch()
    {
        if (Input.touchCount == 0) { hadSingleTouchLastFrame = false; return; }

        // UI üstü dokunuşları kameraya yansıtma
        if (ignoreTouchesOverUI)
        {
            for (int i = 0; i < Input.touchCount; i++)
                if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(Input.touches[i].fingerId))
                    return;
        }

        if (Input.touchCount >= 2) // PINCH ZOOM (telefon/tablet)
        {
            var t0 = Input.touches[0];
            var t1 = Input.touches[1];

            // önceki ve şimdiki iki parmak arası mesafe farkı
            var prevDist = (t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition);
            var currDist = t0.position - t1.position;
            float deltaMag = currDist.magnitude - prevDist.magnitude;

            // parmaklar açılıyorsa deltaMag > 0 → zoom in (orthoSize küçülür)
            targetOrtho = Mathf.Clamp(
                cam.orthographicSize - deltaMag * zoomSpeed * Time.deltaTime * 60f,
                minOrthoSize, maxOrthoSize
            );

            hadSingleTouchLastFrame = false;
            changedThisFrame = true;
        }
        else // TEK PARMAK: PAN
        {
            var t = Input.touches[0];
            if (t.phase == TouchPhase.Began || !hadSingleTouchLastFrame)
            {
                lastTouchPos = t.position;
                hadSingleTouchLastFrame = true;
                return;
            }

            Vector3 wpNow = cam.ScreenToWorldPoint(new Vector3(t.position.x, t.position.y, 0f));
            Vector3 wpPrev = cam.ScreenToWorldPoint(new Vector3(lastTouchPos.x, lastTouchPos.y, 0f));
            Vector3 delta = (wpPrev - wpNow) * panSpeed;

            if (delta.sqrMagnitude > 0f)
            {
                transform.position += delta;
                changedThisFrame = true;
            }
            lastTouchPos = t.position;
        }
    }

    void ClampCameraToBounds()
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float minX = worldBounds.xMin + halfW;
        float maxX = worldBounds.xMax - halfW;
        float minY = worldBounds.yMin + halfH;
        float maxY = worldBounds.yMax - halfH;

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        transform.position = p;
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    void MouseEmulation()
    {
        // --- SCROLL ZOOM (hem yeni hem eski input) ---
        float scroll = 0f;

        // Eski Input Manager (trackpadlerde daha güvenilir olabiliyor)
#if ENABLE_LEGACY_INPUT_MANAGER
        scroll = Input.GetAxis("Mouse ScrollWheel");   // -1..+1, trackpadde küçük değerler
#endif
        // Yeni input (mouseScrollDelta) – eğer yukarıdaki 0 geldiyse buradan dene
        if (Mathf.Approximately(scroll, 0f))
            scroll = Input.mouseScrollDelta.y * 0.1f;  // yüksek çözünürlükte normalize et

        if (Mathf.Abs(scroll) > 0.00001f)
        {
            // ileri = zoom in (ortho küçülür), geri = zoom out
            targetOrtho = Mathf.Clamp(
                cam.orthographicSize - scroll * scrollZoomStep,
                minOrthoSize, maxOrthoSize
            );
            changedThisFrame = true;
        }

        // --- ORTA TUŞ PAN ---
        if (Input.GetMouseButtonDown(2)) lastTouchPos = Input.mousePosition;
        if (Input.GetMouseButton(2))
        {
            Vector2 now = Input.mousePosition;
            Vector3 wpNow = cam.ScreenToWorldPoint(new Vector3(now.x, now.y, 0f));
            Vector3 wpPrev = cam.ScreenToWorldPoint(new Vector3(lastTouchPos.x, lastTouchPos.y, 0f));
            Vector3 delta = (wpPrev - wpNow) * panSpeed;

            if (delta.sqrMagnitude > 0f)
            {
                transform.position += delta;
                changedThisFrame = true;
            }
            lastTouchPos = now;
        }
    }
#endif
}
